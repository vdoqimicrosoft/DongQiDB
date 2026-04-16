using DongQiDB.Domain.Common;

namespace DongQiDB.Api.DTOs.Connections;

/// <summary>
/// Connection list item
/// </summary>
public class ConnectionListItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DatabaseType DatabaseType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Connection detail (without password)
/// </summary>
public class ConnectionDetailDto : ConnectionListItemDto
{
}

/// <summary>
/// Create connection request
/// </summary>
public class CreateConnectionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DatabaseType DatabaseType { get; set; }
}

/// <summary>
/// Update connection request
/// </summary>
public class UpdateConnectionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public DatabaseType DatabaseType { get; set; }
}

/// <summary>
/// Test connection request
/// </summary>
public class TestConnectionRequest
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DatabaseType DatabaseType { get; set; }
}

/// <summary>
/// Test connection response
/// </summary>
public class TestConnectionResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
