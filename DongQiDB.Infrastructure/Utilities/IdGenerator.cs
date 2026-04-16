namespace DongQiDB.Infrastructure.Utilities;

/// <summary>
/// Distributed ID generator using snowflake algorithm
/// </summary>
public static class IdGenerator
{
    private static long _lastTimestamp = 0;
    private static long _sequence = 0;
    private const long SequenceMask = 4095;
    private const int TimestampShift = 22;
    private const int WorkerIdShift = 12;

    public static long Generate()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        lock (typeof(IdGenerator))
        {
            if (timestamp < _lastTimestamp)
                throw new InvalidOperationException("Clock moved backwards");

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & SequenceMask;
                if (_sequence == 0)
                    timestamp = WaitNextMillis(_lastTimestamp);
            }
            else
            {
                _sequence = 0;
            }

            _lastTimestamp = timestamp;
            return (timestamp << TimestampShift) | _sequence;
        }
    }

    private static long WaitNextMillis(long lastTimestamp)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        while (timestamp <= lastTimestamp)
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return timestamp;
    }
}
