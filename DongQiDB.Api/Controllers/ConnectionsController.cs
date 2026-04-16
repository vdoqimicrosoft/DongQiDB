using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DongQiDB.Api.DTOs.Connections;
using DongQiDB.Application.DTOs;
using DongQiDB.Application.Interfaces;
using DongQiDB.Domain.Common;
using DongQiDB.Domain.Entities;

namespace DongQiDB.Api.Controllers;

/// <summary>
/// Connection management controller
/// </summary>
[ApiController]
[Route("api/v1/connections")]
[ApiVersion("1")]
[Authorize]
public class ConnectionsController : ControllerBase
{
    private readonly IConnectionService _connectionService;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<ConnectionsController> _logger;

    public ConnectionsController(
        IConnectionService connectionService,
        IConnectionManager connectionManager,
        ILogger<ConnectionsController> logger)
    {
        _connectionService = connectionService;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all connections
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Result<IEnumerable<ConnectionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var connections = await _connectionService.GetAllAsync(cancellationToken);
        var dtos = connections.Select(MapToListItem);
        return Ok(Result<IEnumerable<ConnectionListItemDto>>.Ok(dtos));
    }

    /// <summary>
    /// Get connection by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<ConnectionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ConnectionDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var connection = await _connectionService.GetByIdAsync(id, cancellationToken);
        if (connection == null)
        {
            return NotFound(Result<ConnectionDetailDto>.Fail(ErrorCode.NotFound, $"Connection {id} not found"));
        }

        return Ok(Result<ConnectionDetailDto>.Ok(MapToDetail(connection)));
    }

    /// <summary>
    /// Create a new connection
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Result<ConnectionDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<ConnectionDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateConnectionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Host))
        {
            return BadRequest(Result<ConnectionDetailDto>.Fail(
                ErrorCode.ValidationFailed,
                "Name and Host are required"));
        }

        var connection = new Connection
        {
            Name = request.Name,
            Host = request.Host,
            Port = request.Port,
            Database = request.Database,
            Username = request.Username,
            DatabaseType = request.DatabaseType
        };

        var created = await _connectionService.CreateAsync(connection, request.Password, cancellationToken);
        _logger.LogInformation("Created connection {ConnectionId}: {Name}", created.Id, created.Name);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            Result<ConnectionDetailDto>.Ok(MapToDetail(created)));
    }

    /// <summary>
    /// Update an existing connection
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Result<ConnectionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ConnectionDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateConnectionRequest request, CancellationToken cancellationToken)
    {
        var existing = await _connectionService.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return NotFound(Result<ConnectionDetailDto>.Fail(ErrorCode.NotFound, $"Connection {id} not found"));
        }

        existing.Name = request.Name;
        existing.Host = request.Host;
        existing.Port = request.Port;
        existing.Database = request.Database;
        existing.Username = request.Username;
        existing.DatabaseType = request.DatabaseType;

        var updated = await _connectionService.UpdateAsync(existing, request.Password, cancellationToken);
        if (updated == null)
        {
            return NotFound(Result<ConnectionDetailDto>.Fail(ErrorCode.NotFound, $"Connection {id} not found"));
        }

        _logger.LogInformation("Updated connection {ConnectionId}", id);
        return Ok(Result<ConnectionDetailDto>.Ok(MapToDetail(updated)));
    }

    /// <summary>
    /// Delete a connection
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var deleted = await _connectionService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(Result<bool>.Fail(ErrorCode.NotFound, $"Connection {id} not found"));
        }

        _logger.LogInformation("Deleted connection {ConnectionId}", id);
        return Ok(Result<bool>.Ok(true));
    }

    /// <summary>
    /// Test a connection
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(typeof(Result<TestConnectionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TestConnectionResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TestConnection([FromBody] TestConnectionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Host))
        {
            return BadRequest(Result<TestConnectionResponse>.Fail(
                ErrorCode.ValidationFailed,
                "Host is required"));
        }

        var connection = new Connection
        {
            Host = request.Host,
            Port = request.Port,
            Database = request.Database,
            Username = request.Username,
            DatabaseType = request.DatabaseType
        };

        var (success, errorMessage) = await _connectionManager.TestConnectionAsync(
            connection, request.Password, cancellationToken);

        var result = new TestConnectionResponse
        {
            Success = success,
            ErrorMessage = errorMessage
        };

        if (!success)
        {
            _logger.LogWarning("Connection test failed: {Error}", errorMessage);
            return Ok(Result<TestConnectionResponse>.Ok(result));
        }

        return Ok(Result<TestConnectionResponse>.Ok(result));
    }

    private static ConnectionListItemDto MapToListItem(Connection connection) => new()
    {
        Id = connection.Id,
        Name = connection.Name,
        Host = connection.Host,
        Port = connection.Port,
        Database = connection.Database,
        Username = connection.Username,
        DatabaseType = connection.DatabaseType,
        CreatedAt = connection.CreatedAt,
        UpdatedAt = connection.UpdatedAt
    };

    private static ConnectionDetailDto MapToDetail(Connection connection) => new()
    {
        Id = connection.Id,
        Name = connection.Name,
        Host = connection.Host,
        Port = connection.Port,
        Database = connection.Database,
        Username = connection.Username,
        DatabaseType = connection.DatabaseType,
        CreatedAt = connection.CreatedAt,
        UpdatedAt = connection.UpdatedAt
    };
}
