using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DongQiDB.Api.DTOs.DDL;
using DongQiDB.Application.DTOs;
using DongQiDB.Application.Interfaces;
using DongQiDB.Domain.Common;

namespace DongQiDB.Api.Controllers;

/// <summary>
/// DDL operations controller for index management
/// </summary>
[ApiController]
[Route("api/v1/ddl")]
[ApiVersion("1")]
[Authorize]
public class DdlController : ControllerBase
{
    private readonly IConnectionService _connectionService;
    private readonly ISchemaService _schemaService;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<DdlController> _logger;

    public DdlController(
        IConnectionService connectionService,
        ISchemaService schemaService,
        IConnectionManager connectionManager,
        ILogger<DdlController> logger)
    {
        _connectionService = connectionService;
        _schemaService = schemaService;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all indexes for a connection
    /// </summary>
    [HttpGet("indexes")]
    [ProducesResponseType(typeof(Result<IEnumerable<IndexInfoDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IEnumerable<IndexInfoDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIndexes([FromQuery] long connectionId, [FromQuery] string? schemaName, [FromQuery] string? tableName, CancellationToken cancellationToken)
    {
        var connection = await _connectionService.GetByIdAsync(connectionId, cancellationToken);
        if (connection == null)
        {
            return NotFound(Result<IEnumerable<IndexInfoDto>>.Fail(
                ErrorCode.NotFound,
                $"Connection {connectionId} not found"));
        }

        var password = DecryptPassword(connection.EncryptedPassword);
        var tables = await _schemaService.GetTablesAsync(connectionId, password, schemaName, cancellationToken);

        var allIndexes = new List<IndexInfoDto>();

        foreach (var table in tables)
        {
            if (!string.IsNullOrEmpty(tableName) && table.TableName != tableName)
                continue;

            var indexes = await _schemaService.GetIndexesAsync(connectionId, table.TableName, password, table.SchemaName, cancellationToken);

            allIndexes.AddRange(indexes.Select(i => new IndexInfoDto
            {
                Name = i.IndexName,
                SchemaName = table.SchemaName,
                TableName = table.TableName,
                Columns = string.IsNullOrEmpty(i.Columns)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(i.Columns) ?? new List<string>(),
                IsUnique = i.IsUnique,
                IsPrimaryKey = i.IsPrimaryKey
            }));
        }

        return Ok(Result<IEnumerable<IndexInfoDto>>.Ok(allIndexes));
    }

    /// <summary>
    /// Create a new index
    /// </summary>
    [HttpPost("indexes")]
    [ProducesResponseType(typeof(Result<CreateIndexResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CreateIndexResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<CreateIndexResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateIndex([FromBody] CreateIndexRequest request, CancellationToken cancellationToken)
    {
        if (request.ConnectionId <= 0 || string.IsNullOrEmpty(request.TableName) || request.Columns.Count == 0)
        {
            return BadRequest(Result<CreateIndexResponse>.Fail(
                ErrorCode.ValidationFailed,
                "ConnectionId, TableName, and Columns are required"));
        }

        var connection = await _connectionService.GetByIdAsync(request.ConnectionId, cancellationToken);
        if (connection == null)
        {
            return NotFound(Result<CreateIndexResponse>.Fail(
                ErrorCode.NotFound,
                $"Connection {request.ConnectionId} not found"));
        }

        // Generate DDL based on database type
        var ddl = GenerateCreateIndexDdl(connection.DatabaseType, request);

        _logger.LogInformation("Generated CREATE INDEX DDL for connection {ConnectionId}", request.ConnectionId);

        return Ok(Result<CreateIndexResponse>.Ok(new CreateIndexResponse
        {
            Success = true,
            Ddl = ddl
        }));
    }

    /// <summary>
    /// Delete an index
    /// </summary>
    [HttpDelete("indexes/{name}")]
    [ProducesResponseType(typeof(Result<DeleteIndexResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<DeleteIndexResponse>), StatusCodes.Status400BadRequest)]
    public IActionResult DeleteIndex(string name, [FromQuery] long connectionId, [FromQuery] string? schemaName, [FromQuery] string? tableName, CancellationToken cancellationToken)
    {
        if (connectionId <= 0 || string.IsNullOrEmpty(name))
        {
            return BadRequest(Result<DeleteIndexResponse>.Fail(
                ErrorCode.ValidationFailed,
                "ConnectionId and IndexName are required"));
        }

        // Generate DDL based on database type
        // In production, this would actually execute the DDL
        var ddl = $"DROP INDEX {(string.IsNullOrEmpty(schemaName) ? "" : schemaName + ".")}{name}";

        _logger.LogInformation("Generated DROP INDEX DDL: {Ddl}", ddl);

        return Ok(Result<DeleteIndexResponse>.Ok(new DeleteIndexResponse
        {
            Success = true,
            Ddl = ddl
        }));
    }

    private static string GenerateCreateIndexDdl(Domain.Common.DatabaseType databaseType, CreateIndexRequest request)
    {
        var indexName = string.IsNullOrEmpty(request.IndexName)
            ? $"idx_{request.TableName}_{string.Join("_", request.Columns)}"
            : request.IndexName;
        var columns = string.Join(", ", request.Columns.Select(c => $"\"{c}\""));
        var unique = request.IsUnique ? "UNIQUE " : "";

        return databaseType switch
        {
            Domain.Common.DatabaseType.PostgreSql => $"CREATE {unique}INDEX \"{indexName}\" ON \"{request.SchemaName}\".\"{request.TableName}\" ({columns})",
            Domain.Common.DatabaseType.MySql => $"CREATE {unique}INDEX `{indexName}` ON `{request.TableName}` ({columns})",
            Domain.Common.DatabaseType.SqlServer => $"CREATE {unique}INDEX [{indexName}] ON [{request.SchemaName}].[{request.TableName}] ({columns})",
            Domain.Common.DatabaseType.Sqlite => $"CREATE {unique}INDEX IF NOT EXISTS \"{indexName}\" ON \"{request.TableName}\" ({columns})",
            _ => $"CREATE {unique}INDEX {indexName} ON {request.TableName} ({columns})"
        };
    }

    private static string GetEncryptionKey() => "DongQiDB32ByteEncryptionKey123456";
    private static string GetEncryptionIv() => "DongQiDB16Byte";

    private string DecryptPassword(string encryptedPassword)
    {
        try
        {
            return _connectionManager.DecryptPassword(encryptedPassword, GetEncryptionKey(), GetEncryptionIv());
        }
        catch
        {
            return string.Empty;
        }
    }
}
