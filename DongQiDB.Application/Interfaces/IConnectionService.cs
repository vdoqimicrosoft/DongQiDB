using DongQiDB.Domain.Entities;

namespace DongQiDB.Application.Interfaces;

/// <summary>
/// Connection service interface
/// </summary>
public interface IConnectionService
{
    /// <summary>
    /// Gets all connections
    /// </summary>
    Task<IEnumerable<Connection>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets connection by ID
    /// </summary>
    Task<Connection?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new connection
    /// </summary>
    Task<Connection> CreateAsync(Connection connection, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing connection
    /// </summary>
    Task<Connection?> UpdateAsync(Connection connection, string? newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a connection
    /// </summary>
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests a connection
    /// </summary>
    Task<(bool Success, string? ErrorMessage, long ResponseTimeMs)> TestConnectionAsync(
        long connectionId,
        string? testPassword = null,
        CancellationToken cancellationToken = default);
}
