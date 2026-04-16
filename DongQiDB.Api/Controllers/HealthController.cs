using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DongQiDB.Infrastructure.Data;

namespace DongQiDB.Api.Controllers;

/// <summary>
/// Health check controller
/// </summary>
[ApiController]
[Route("health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly SystemDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(SystemDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new HealthResponse
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }

    /// <summary>
    /// Readiness check - verifies database connectivity
    /// </summary>
    [HttpGet("ready")]
    [ProducesResponseType(typeof(ReadinessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReadinessResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var checks = new List<HealthCheckItem>();
        var isReady = true;

        // Database check
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            checks.Add(new HealthCheckItem
            {
                Name = "database",
                Status = canConnect ? "healthy" : "unhealthy",
                Description = canConnect ? "Database connection successful" : "Database connection failed"
            });
            if (!canConnect) isReady = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            checks.Add(new HealthCheckItem
            {
                Name = "database",
                Status = "unhealthy",
                Description = ex.Message
            });
            isReady = false;
        }

        var response = new ReadinessResponse
        {
            Status = isReady ? "ready" : "not_ready",
            Timestamp = DateTime.UtcNow,
            Checks = checks
        };

        return isReady ? Ok(response) : StatusCode(503, response);
    }

    /// <summary>
    /// Liveness check - verifies application is running
    /// </summary>
    [HttpGet("live")]
    [ProducesResponseType(typeof(LivenessResponse), StatusCodes.Status200OK)]
    public IActionResult Live()
    {
        return Ok(new LivenessResponse
        {
            Status = "alive",
            Timestamp = DateTime.UtcNow,
            UptimeSeconds = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds
        });
    }
}

/// <summary>
/// Basic health response
/// </summary>
public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Readiness response with checks
/// </summary>
public class ReadinessResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<HealthCheckItem> Checks { get; set; } = new();
}

/// <summary>
/// Health check item
/// </summary>
public class HealthCheckItem
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Liveness response
/// </summary>
public class LivenessResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double UptimeSeconds { get; set; }
}
