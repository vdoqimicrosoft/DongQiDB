using Microsoft.Extensions.Logging;
using DongQiDB.Application.Interfaces;
using DongQiDB.Domain.Common;
using DongQiDB.Domain.Entities;
using DongQiDB.Infrastructure.Configuration;
using DongQiDB.Infrastructure.Utilities;

namespace DongQiDB.Infrastructure.Services;

/// <summary>
/// Connection manager implementation
/// </summary>
public class ConnectionManager : IConnectionManager
{
    private readonly AppSettings _appSettings;
    private readonly ILogger<ConnectionManager> _logger;

    public ConnectionManager(AppSettings appSettings, ILogger<ConnectionManager> logger)
    {
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task<(bool Success, string? ErrorMessage)> TestConnectionAsync(
        Connection connection,
        string decryptedPassword,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = GetConnectionString(connection, decryptedPassword);
            await using var dbConnection = CreateDbConnection(connection.DatabaseType, connectionString);
            await dbConnection.OpenAsync(cancellationToken);
            _logger.LogInformation("Connection test successful for {ConnectionName}", connection.Name);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for {ConnectionName}: {Error}", connection.Name, ex.Message);
            return (false, ex.Message);
        }
    }

    public string EncryptPassword(string password, string key, string iv)
    {
        if (string.IsNullOrEmpty(password))
            return string.Empty;

        return CryptoHelper.AesEncrypt(password, key, iv);
    }

    public string DecryptPassword(string encryptedPassword, string key, string iv)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            return string.Empty;

        return CryptoHelper.AesDecrypt(encryptedPassword, key, iv);
    }

    public string GetConnectionString(Connection connection, string decryptedPassword)
    {
        return connection.DatabaseType switch
        {
            DatabaseType.PostgreSql => GetPostgreSqlConnectionString(connection, decryptedPassword),
            DatabaseType.Sqlite => GetSqliteConnectionString(connection),
            _ => throw new NotSupportedException($"Database type {connection.DatabaseType} is not supported")
        };
    }

    public string BuildConnectionString(Connection connection, string decryptedPassword)
    {
        return GetConnectionString(connection, decryptedPassword);
    }

    private string GetPostgreSqlConnectionString(Connection connection, string decryptedPassword)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = connection.Host,
            Port = connection.Port,
            Database = connection.Database,
            Username = connection.Username,
            Password = decryptedPassword,
            Timeout = _appSettings.Database.ConnectionPool.ConnectionTimeout,
            Pooling = true,
            MinPoolSize = _appSettings.Database.ConnectionPool.MinSize,
            MaxPoolSize = _appSettings.Database.ConnectionPool.MaxSize
        };

        return builder.ConnectionString;
    }

    private string GetSqliteConnectionString(Connection connection)
    {
        // For SQLite, the Database field contains the file path
        return $"Data Source={connection.Database};";
    }

    private IAsyncDisposableConnection CreateDbConnection(DatabaseType databaseType, string connectionString)
    {
        return databaseType switch
        {
            DatabaseType.PostgreSql => new NpgsqlConnectionWrapper(connectionString),
            DatabaseType.Sqlite => new SqliteConnectionWrapper(connectionString),
            _ => throw new NotSupportedException($"Database type {databaseType} is not supported")
        };
    }
}

public interface IAsyncDisposableConnection : IAsyncDisposable
{
    string ConnectionString { get; }
    System.Data.ConnectionState State { get; }
    Task OpenAsync(CancellationToken cancellationToken = default);
}

public class NpgsqlConnectionWrapper : IAsyncDisposableConnection
{
    private readonly Npgsql.NpgsqlConnection _connection;

    public NpgsqlConnectionWrapper(string connectionString)
    {
        _connection = new Npgsql.NpgsqlConnection(connectionString);
    }

    public string ConnectionString => _connection.ConnectionString;
    public System.Data.ConnectionState State => _connection.State;

    public Task OpenAsync(CancellationToken cancellationToken = default)
        => _connection.OpenAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}

public class SqliteConnectionWrapper : IAsyncDisposableConnection
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;

    public SqliteConnectionWrapper(string connectionString)
    {
        _connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
    }

    public string ConnectionString => _connection.ConnectionString;
    public System.Data.ConnectionState State => _connection.State;

    public Task OpenAsync(CancellationToken cancellationToken = default)
        => _connection.OpenAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}