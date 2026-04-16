using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DongQiDB.Application.Interfaces;
using DongQiDB.Domain.Entities;
using DongQiDB.Infrastructure.Data;

namespace DongQiDB.Infrastructure.Services;

/// <summary>
/// Connection service implementation
/// </summary>
public class ConnectionService : IConnectionService
{
    private readonly SystemDbContext _context;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<ConnectionService> _logger;
    private readonly string _encryptionKey;
    private readonly string _encryptionIv;

    public ConnectionService(
        SystemDbContext context,
        IConnectionManager connectionManager,
        ILogger<ConnectionService> logger)
    {
        _context = context;
        _connectionManager = connectionManager;
        _logger = logger;
        // In production, use proper key management
        _encryptionKey = "DongQiDB32ByteEncryptionKey123456";
        _encryptionIv = "DongQiDB16Byte";
    }

    public async Task<IEnumerable<Connection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Connections
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Connection?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Connections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Connection> CreateAsync(Connection connection, string password, CancellationToken cancellationToken = default)
    {
        connection.EncryptedPassword = _connectionManager.EncryptPassword(password, _encryptionKey, _encryptionIv);
        _context.Connections.Add(connection);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created connection {ConnectionId}: {ConnectionName}", connection.Id, connection.Name);
        return connection;
    }

    public async Task<Connection?> UpdateAsync(Connection connection, string? newPassword, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Connections.FindAsync(new object[] { connection.Id }, cancellationToken);
        if (existing == null) return null;

        existing.Name = connection.Name;
        existing.Host = connection.Host;
        existing.Port = connection.Port;
        existing.Database = connection.Database;
        existing.Username = connection.Username;
        existing.DatabaseType = connection.DatabaseType;

        if (!string.IsNullOrEmpty(newPassword))
        {
            existing.EncryptedPassword = _connectionManager.EncryptPassword(newPassword, _encryptionKey, _encryptionIv);
        }

        existing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated connection {ConnectionId}", connection.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var connection = await _context.Connections.FindAsync(new object[] { id }, cancellationToken);
        if (connection == null) return false;

        _context.Connections.Remove(connection);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deleted connection {ConnectionId}", id);
        return true;
    }

    public async Task<(bool Success, string? ErrorMessage, long ResponseTimeMs)> TestConnectionAsync(
        long connectionId,
        string? testPassword = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await _context.Connections.FindAsync(new object[] { connectionId }, cancellationToken);
        if (connection == null)
        {
            return (false, "Connection not found", 0);
        }

        var password = testPassword ?? _connectionManager.DecryptPassword(connection.EncryptedPassword, _encryptionKey, _encryptionIv);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var (success, errorMessage) = await _connectionManager.TestConnectionAsync(connection, password, cancellationToken);
            stopwatch.Stop();
            return (success, errorMessage, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to test connection {ConnectionId}", connectionId);
            return (false, ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }
}
