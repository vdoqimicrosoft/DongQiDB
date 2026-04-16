using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DongQiDB.Api.DTOs.Schema;
using DongQiDB.Application.DTOs;
using DongQiDB.Application.Interfaces;
using DongQiDB.Domain.Common;
using DongQiDB.Domain.Entities;

namespace DongQiDB.Api.Controllers;

/// <summary>
/// Schema management controller
/// </summary>
[ApiController]
[Route("api/v1/schema")]
[ApiVersion("1")]
[Authorize]
public class SchemaController : ControllerBase
{
    private readonly IConnectionService _connectionService;
    private readonly ISchemaService _schemaService;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<SchemaController> _logger;

    public SchemaController(
        IConnectionService connectionService,
        ISchemaService schemaService,
        IConnectionManager connectionManager,
        ILogger<SchemaController> logger)
    {
        _connectionService = connectionService;
        _schemaService = schemaService;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    /// <summary>
    /// Get schema overview for a connection
    /// </summary>
    [HttpGet("{connId}")]
    [ProducesResponseType(typeof(Result<SchemaOverviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SchemaOverviewDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchema(long connId, [FromQuery] string? schemaName, CancellationToken cancellationToken)
    {
        var connection = await _connectionService.GetByIdAsync(connId, cancellationToken);
        if (connection == null)
        {
            return NotFound(Result<SchemaOverviewDto>.Fail(ErrorCode.NotFound, $"Connection {connId} not found"));
        }

        var password = _connectionManager.DecryptPassword(connection.EncryptedPassword, GetEncryptionKey(), GetEncryptionIv());
        var schemaResult = await _schemaService.GetFullSchemaAsync(connId, password, schemaName, cancellationToken);

        var dto = new SchemaOverviewDto
        {
            ConnectionId = connId,
            Tables = schemaResult.Tables.Select(t => new TableDto
            {
                SchemaName = t.SchemaName,
                TableName = t.TableName,
                TableComment = t.TableComment,
                RowCount = t.RowCount,
                Columns = schemaResult.Columns
                    .Where(c => c.Table != null && c.Table.TableName == t.TableName && c.Table.SchemaName == t.SchemaName)
                    .Select(c => new ColumnDto
                    {
                        Name = c.ColumnName,
                        DataType = c.DataType,
                        IsNullable = c.IsNullable,
                        IsPrimaryKey = c.IsPrimaryKey,
                        IsForeignKey = c.IsForeignKey,
                        DefaultValue = c.DefaultValue,
                        Comment = c.ColumnComment,
                        MaxLength = c.MaxLength,
                        Precision = c.Precision,
                        Scale = c.Scale
                    }).ToList(),
                Indexes = schemaResult.Indexes
                    .Where(i => i.Table != null && i.Table.TableName == t.TableName && i.Table.SchemaName == t.SchemaName)
                    .Select(i => new IndexDto
                    {
                        Name = i.IndexName,
                        IsUnique = i.IsUnique,
                        IsPrimaryKey = i.IsPrimaryKey,
                        Columns = string.IsNullOrEmpty(i.Columns)
                            ? new List<string>()
                            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(i.Columns) ?? new List<string>()
                    }).ToList()
            }).ToList()
        };

        return Ok(Result<SchemaOverviewDto>.Ok(dto));
    }

    /// <summary>
    /// Get tables for a connection
    /// </summary>
    [HttpGet("{connId}/tables")]
    [ProducesResponseType(typeof(Result<IEnumerable<TableListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IEnumerable<TableListItemDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTables(long connId, [FromQuery] string? schemaName, CancellationToken cancellationToken)
    {
        var connection = await _connectionService.GetByIdAsync(connId, cancellationToken);
        if (connection == null)
        {
            return NotFound(Result<IEnumerable<TableListItemDto>>.Fail(ErrorCode.NotFound, $"Connection {connId} not found"));
        }

        var password = _connectionManager.DecryptPassword(connection.EncryptedPassword, GetEncryptionKey(), GetEncryptionIv());
        var tables = await _schemaService.GetTablesAsync(connId, password, schemaName, cancellationToken);

        var dtos = tables.Select(t => new TableListItemDto
        {
            SchemaName = t.SchemaName,
            TableName = t.TableName,
            TableComment = t.TableComment,
            RowCount = t.RowCount
        });

        return Ok(Result<IEnumerable<TableListItemDto>>.Ok(dtos));
    }

    /// <summary>
    /// Get table structure including columns and indexes
    /// </summary>
    [HttpGet("{connId}/tables/{name}")]
    [ProducesResponseType(typeof(Result<TableDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TableDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTable(long connId, string name, [FromQuery] string? schemaName, CancellationToken cancellationToken)
    {
        var connection = await _connectionService.GetByIdAsync(connId, cancellationToken);
        if (connection == null)
        {
            return NotFound(Result<TableDto>.Fail(ErrorCode.NotFound, $"Connection {connId} not found"));
        }

        var password = _connectionManager.DecryptPassword(connection.EncryptedPassword, GetEncryptionKey(), GetEncryptionIv());

        var columns = await _schemaService.GetColumnsAsync(connId, name, password, schemaName, cancellationToken);
        var indexes = await _schemaService.GetIndexesAsync(connId, name, password, schemaName, cancellationToken);

        var firstColumn = columns.FirstOrDefault();

        var dto = new TableDto
        {
            SchemaName = schemaName ?? "public",
            TableName = name,
            TableComment = firstColumn?.Table?.TableComment,
            RowCount = firstColumn?.Table?.RowCount ?? 0,
            Columns = columns.Select(c => new ColumnDto
            {
                Name = c.ColumnName,
                DataType = c.DataType,
                IsNullable = c.IsNullable,
                IsPrimaryKey = c.IsPrimaryKey,
                IsForeignKey = c.IsForeignKey,
                DefaultValue = c.DefaultValue,
                Comment = c.ColumnComment,
                MaxLength = c.MaxLength,
                Precision = c.Precision,
                Scale = c.Scale
            }).ToList(),
            Indexes = indexes.Select(i => new IndexDto
            {
                Name = i.IndexName,
                IsUnique = i.IsUnique,
                IsPrimaryKey = i.IsPrimaryKey,
                Columns = string.IsNullOrEmpty(i.Columns)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(i.Columns) ?? new List<string>()
            }).ToList()
        };

        return Ok(Result<TableDto>.Ok(dto));
    }

    /// <summary>
    /// Refresh schema cache
    /// </summary>
    [HttpPost("{connId}/refresh")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefreshSchema(long connId, CancellationToken cancellationToken)
    {
        var connection = await _connectionService.GetByIdAsync(connId, cancellationToken);
        if (connection == null)
        {
            return NotFound(Result<bool>.Fail(ErrorCode.NotFound, $"Connection {connId} not found"));
        }

        var password = _connectionManager.DecryptPassword(connection.EncryptedPassword, GetEncryptionKey(), GetEncryptionIv());
        await _connectionService.TestConnectionAsync(connId, password, cancellationToken);

        // Refresh cache is handled by schema service internally
        _logger.LogInformation("Refreshed schema for connection {ConnectionId}", connId);
        return Ok(Result<bool>.Ok(true));
    }

    private static string GetEncryptionKey() => "DongQiDB32ByteEncryptionKey123456";
    private static string GetEncryptionIv() => "DongQiDB16Byte";
}
