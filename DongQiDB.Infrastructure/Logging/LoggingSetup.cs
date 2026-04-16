using Serilog;
using Serilog.Events;

namespace DongQiDB.Infrastructure.Logging;

/// <summary>
/// Serilog configuration setup
/// </summary>
public static class LoggingSetup
{
    public static ILogger CreateLogger(string logDirectory, bool consoleEnabled, bool fileEnabled, int retainedDays)
    {
        var config = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "DongQiDB");

        if (consoleEnabled)
            config.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        if (fileEnabled)
            config.WriteTo.File(
                $"{logDirectory}/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retainedDays,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        return config.CreateLogger();
    }
}
