using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;

namespace DongQiDB.Infrastructure.Logging;

/// <summary>
/// Request/Response logging middleware
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("Method", context.Request.Method))
        using (LogContext.PushProperty("Path", context.Request.Path))
        {
            Log.Information("Request started: {Method} {Path}");

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                using (LogContext.PushProperty("StatusCode", context.Response.StatusCode))
                using (LogContext.PushProperty("ElapsedMs", stopwatch.ElapsedMilliseconds))
                {
                    Log.Information("Request completed: {StatusCode} in {ElapsedMs}ms");
                }
            }
        }
    }
}
