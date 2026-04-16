using DongQiDB.Domain.Entities;

namespace DongQiDB.Application.Interfaces;

/// <summary>
/// Connection manager interface
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// Tests database connection
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> TestConnectionAsync(
        Connection connection,
        string decryptedPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts password
    /// </summary>
    string EncryptPassword(string password, string key, string iv);

    /// <summary>
    /// Decrypts password
    /// </summary>
    string DecryptPassword(string encryptedPassword, string key, string iv);

    /// <summary>
    /// Gets connection string for a database type
    /// </summary>
    string GetConnectionString(Connection connection, string decryptedPassword);

    /// <summary>
    /// Builds connection string from connection entity
    /// </summary>
    string BuildConnectionString(Connection connection, string decryptedPassword);
}
