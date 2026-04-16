using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DongQiDB.Application.Interfaces;
using DongQiDB.Infrastructure.AI;
using DongQiDB.Infrastructure.Configuration;
using DongQiDB.Infrastructure.Data;
using DongQiDB.Infrastructure.Services;
using DongQiDB.Infrastructure.Services.Export;
using DongQiDB.Infrastructure.Services.SchemaReaders;

namespace DongQiDB.Infrastructure.DependencyInjection;

/// <summary>
/// Service collection extensions
/// </summary>
public static class ServiceExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, AppSettings appSettings)
    {
        // Register DbContext based on provider
        services.AddDbContext<SystemDbContext>(options =>
        {
            var provider = appSettings.Database.Provider.ToLower();
            var connectionString = $"Data Source={appSettings.Database.Name}.db";

            switch (provider)
            {
                case "postgresql":
                    // For system database, use SQLite by default
                    options.UseSqlite(connectionString);
                    break;
                case "sqlite":
                default:
                    options.UseSqlite(connectionString);
                    break;
            }
        });

        // Register services
        services.AddSingleton(appSettings);
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddScoped<IConnectionService, ConnectionService>();
        services.AddScoped<ISchemaService, SchemaService>();

        // M2 Query services
        services.AddSingleton<ISqlValidator, SqlValidator>();
        services.AddScoped<IQueryExecutor, QueryExecutor>();
        services.AddScoped<IQueryHistoryService, QueryHistoryService>();
        services.AddSingleton<ISqlFormatter, SqlFormatter>();

        // Register schema readers
        services.AddTransient<PostgreSqlSchemaReader>();
        services.AddTransient<SqliteSchemaReader>();

        // M5 Export services
        services.AddScoped<ICsvExporter, CsvExporter>();
        services.AddScoped<IExcelExporter, ExcelExporter>();

        return services;
    }

    public static IServiceCollection AddAiServices(this IServiceCollection services, AppSettings appSettings)
    {
        // Configure Anthropic API
        var anthropicConfig = new AnthropicConfig
        {
            ApiKey = appSettings.Ai.ApiKey,
            Endpoint = !string.IsNullOrEmpty(appSettings.Ai.Endpoint)
                ? appSettings.Ai.Endpoint
                : "https://api.anthropic.com",
            Model = appSettings.Ai.Model,
            TimeoutSeconds = appSettings.Ai.TimeoutSeconds,
            MaxRetries = appSettings.Ai.MaxRetries,
            EnableStreaming = appSettings.Ai.EnableStreaming
        };

        // Register AI services
        services.AddSingleton(anthropicConfig);
        services.AddSingleton<IAiService, AnthropicAiService>();
        services.AddScoped<ITextToSqlService, TextToSqlService>();
        services.AddScoped<ISqlToTextService, SqlToTextService>();
        services.AddScoped<ISqlOptimizeService, SqlOptimizeService>();
        services.AddScoped<IAiSessionService, AiSessionService>();
        services.AddScoped<IInputFilter, InputFilter>();
        services.AddSingleton<IAiCacheService>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AiCacheService>>();
            return new AiCacheService(logger, useRedis: false);
        });

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}
