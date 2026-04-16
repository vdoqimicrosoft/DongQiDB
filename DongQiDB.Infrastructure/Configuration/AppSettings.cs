namespace DongQiDB.Infrastructure.Configuration;

/// <summary>
/// Application configuration settings
/// </summary>
public class AppSettings
{
    public AppConfig App { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public AiConfig Ai { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public CacheConfig Cache { get; set; } = new();
}

public class AppConfig
{
    public string Name { get; set; } = "DongQiDB";
    public string Version { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Development";
}

public class DatabaseConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3306;
    public string Name { get; set; } = "dongqidb";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30;
    public string Provider { get; set; } = "sqlite";
    public ConnectionPoolConfig ConnectionPool { get; set; } = new();
    public EncryptionConfig Encryption { get; set; } = new();
}

public class ConnectionPoolConfig
{
    public int MinSize { get; set; } = 1;
    public int MaxSize { get; set; } = 10;
    public int IdleTimeout { get; set; } = 300; // seconds
    public int ConnectionTimeout { get; set; } = 30; // seconds
}

public class EncryptionConfig
{
    public string Key { get; set; } = string.Empty;
    public string IV { get; set; } = string.Empty;
}

public class AiConfig
{
    public string Provider { get; set; } = "anthropic";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-20250514";
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxRetries { get; set; } = 3;
    public double Temperature { get; set; } = 0.3;
    public int MaxTokens { get; set; } = 4096;
    public bool EnableStreaming { get; set; } = true;
}

public class LoggingConfig
{
    public bool ConsoleEnabled { get; set; } = true;
    public bool FileEnabled { get; set; } = true;
    public string LogDirectory { get; set; } = "logs";
    public int RetainedFileCount { get; set; } = 30;
}

public class CacheConfig
{
    public string Provider { get; set; } = "memory"; // "memory" or "redis"
    public string? RedisConnectionString { get; set; }
    public int SchemaCacheExpiryHours { get; set; } = 24;
    public int ResultCacheExpiryMinutes { get; set; } = 30;
    public int MaxLruSize { get; set; } = 1000;
}
