namespace DongQiDB.Infrastructure.Utilities;

/// <summary>
/// DateTime utility methods
/// </summary>
public static class DateTimeHelper
{
    public static DateTime UtcNow => DateTime.UtcNow;
    public static DateTime Now => DateTime.Now;

    public static long ToUnixTimestamp(DateTime dateTime)
        => new DateTimeOffset(dateTime).ToUnixTimeSeconds();

    public static DateTime FromUnixTimestamp(long timestamp)
        => DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;

    public static long ToUnixMilliseconds(DateTime dateTime)
        => new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();

    public static DateTime FromUnixMilliseconds(long milliseconds)
        => DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).DateTime;

    public static string ToIso8601(DateTime dateTime)
        => dateTime.ToUniversalTime().ToString("o");

    public static DateTime StartOfDay(DateTime dateTime)
        => dateTime.Date;

    public static DateTime EndOfDay(DateTime dateTime)
        => dateTime.Date.AddDays(1).AddTicks(-1);
}
