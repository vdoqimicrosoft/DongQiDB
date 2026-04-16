using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using DongQiDB.Infrastructure.Configuration;
using DongQiDB.Infrastructure.Data;
using DongQiDB.Infrastructure.DependencyInjection;
using DongQiDB.Infrastructure.Exceptions;
using DongQiDB.Infrastructure.Logging;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using DongQiDB.Api;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var config = ConfigurationLoader.Build(environment);

var settings = new AppSettings();
config.Bind(settings);

Log.Logger = LoggingSetup.CreateLogger(
    settings.Logging.LogDirectory,
    settings.Logging.ConsoleEnabled,
    settings.Logging.FileEnabled,
    settings.Logging.RetainedFileCount);

try
{
    Log.Information("Starting DongQiDB API in {Environment} mode", environment);

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    });

    // Controllers
    builder.Services.AddControllers();

    // Swagger with JWT support
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "DongQiDB API",
            Version = "v1",
            Description = "Text-to-SQL API for DongQiDB",
            Contact = new OpenApiContact
            {
                Name = "DongQiDB Team"
            }
        });

        // JWT Authentication - use query parameter for easier Swagger testing
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT token (will be added as Bearer token)",
            Name = "access_token",
            In = ParameterLocation.Query,
            Type = SecuritySchemeType.ApiKey
        });

        // Make sure all endpoints require authentication by default
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // JWT Authentication
    var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("JWT secret key is not configured");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "DongQiDB";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "DongQiDB";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

    // Authorization Policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAuthenticated", policy =>
            policy.RequireAuthenticatedUser());
    });

    // Rate Limiting - 100 requests per minute
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("fixed", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                }));

        options.OnRejected = async (context, cancellationToken) =>
        {
            Log.Warning("Rate limit exceeded for {Path}", context.HttpContext.Request.Path);
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests. Please try again later.",
                retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? retryAfter.TotalSeconds
                    : 60
            }, cancellationToken);
        };
    });

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });

        options.AddPolicy("AllowSpecific", policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? Array.Empty<string>();

            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // Register application services
    builder.Services.AddDatabaseServices(settings);
    builder.Services.AddAiServices(settings);

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddCheck("database", () =>
        {
            try
            {
                using var scope = builder.Services.BuildServiceProvider().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
                return context.Database.CanConnect()
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy("Database connection failed");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(ex.Message);
            }
        });

    var app = builder.Build();

    // Initialize database
    await app.Services.InitializeDatabaseAsync();

    // Middleware pipeline
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    // Prometheus metrics
    app.UseHttpMetrics();

    // Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DongQiDB API V1");
        c.RoutePrefix = "swagger";
        c.ConfigObject.AdditionalItems["persistAuthorization"] = true;
    });

    // CORS
    var corsPolicy = builder.Configuration["Cors:Policy"] ?? "AllowAll";
    app.UseCors(corsPolicy);

    // Rate Limiting
    app.UseRateLimiter();

    // Support token from query parameter for Swagger testing
    app.Use(async (context, next) =>
    {
        if (context.Request.Query.TryGetValue("access_token", out var token) && !string.IsNullOrEmpty(token))
        {
            context.Request.Headers["Authorization"] = $"Bearer {token}";
        }
        await next();
    });

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Map controllers
    app.MapControllers();

    // Prometheus metrics endpoint
    app.MapMetrics("/metrics");

    // Health check endpoint
    app.MapHealthChecks("/healthz", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds
            });
        }
    });

    Log.Information("DongQiDB API started successfully on port 5000");
    app.Run("http://0.0.0.0:5000");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
