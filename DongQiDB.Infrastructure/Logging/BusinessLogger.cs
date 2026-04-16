using Serilog;
using Serilog.Context;

namespace DongQiDB.Infrastructure.Logging;

/// <summary>
/// Business operation logger
/// </summary>
public interface IBusinessLogger
{
    void LogOperation(string operation, string? userId = null, IDictionary<string, object>? properties = null);
    void LogQuery(string sql, IDictionary<string, object>? parameters = null);
    void LogAiRequest(string prompt, string? model = null);
    void LogAiResponse(string? response, bool success, long elapsedMs = 0);
}

public class BusinessLogger : IBusinessLogger
{
    private readonly Serilog.ILogger _logger;

    public BusinessLogger(Serilog.ILogger logger)
    {
        _logger = logger;
    }

    public void LogOperation(string operation, string? userId = null, IDictionary<string, object>? properties = null)
    {
        using (LogContext.PushProperty("Operation", operation))
        using (LogContext.PushProperty("UserId", userId ?? "System"))
        {
            if (properties is not null)
            {
                foreach (var kvp in properties)
                    LogContext.PushProperty(kvp.Key, kvp.Value);
            }

            _logger.Information("Operation executed: {Operation}");
        }
    }

    public void LogQuery(string sql, IDictionary<string, object>? parameters = null)
    {
        using (LogContext.PushProperty("SqlQuery", sql))
        {
            if (parameters is not null)
                LogContext.PushProperty("Parameters", parameters);

            _logger.Information("Query executed: {SqlQuery}");
        }
    }

    public void LogAiRequest(string prompt, string? model = null)
    {
        using (LogContext.PushProperty("AiModel", model ?? "unknown"))
        using (LogContext.PushProperty("PromptLength", prompt.Length))
        {
            _logger.Information("AI request sent");
        }
    }

    public void LogAiResponse(string? response, bool success, long elapsedMs = 0)
    {
        using (LogContext.PushProperty("AiSuccess", success))
        using (LogContext.PushProperty("AiElapsedMs", elapsedMs))
        {
            _logger.Information("AI response received: {Success}");
        }
    }
}
