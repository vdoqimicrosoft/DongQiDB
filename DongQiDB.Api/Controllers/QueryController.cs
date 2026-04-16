using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DongQiDB.Api.DTOs.Query;
using DongQiDB.Application.DTOs;
using DongQiDB.Application.Interfaces;
using DongQiDB.Domain.Common;

namespace DongQiDB.Api.Controllers;

/// <summary>
/// Query execution controller
/// </summary>
[ApiController]
[Route("api/v1/query")]
[ApiVersion("1")]
[Authorize]
public class QueryController : ControllerBase
{
    private readonly IConnectionService _connectionService;
    private readonly IQueryExecutor _queryExecutor;
    private readonly IConnectionManager _connectionManager;
    private readonly ISqlValidator _sqlValidator;
    private readonly ILogger<QueryController> _logger;

    public QueryController(
        IConnectionService connectionService,
        IQueryExecutor queryExecutor,
        IConnectionManager connectionManager,
        ISqlValidator sqlValidator,
        ILogger<QueryController> logger)
    {
        _connectionService = connectionService;
        _queryExecutor = queryExecutor;
        _connectionManager = connectionManager;
        _sqlValidator = sqlValidator;
        _logger = logger;
    }

    /// <summary>
    /// Execute a SQL query
    /// </summary>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(Result<ExecuteQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ExecuteQueryResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ExecuteQueryResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Execute([FromBody] ExecuteQueryRequest request, CancellationToken cancellationToken)
    {
        // Validate request
        if (request.ConnectionId <= 0 || string.IsNullOrEmpty(request.Sql))
        {
            return BadRequest(Result<ExecuteQueryResponse>.Fail(
                ErrorCode.ValidationFailed,
                "ConnectionId and Sql are required"));
        }

        // Validate SQL
        var validationResult = _sqlValidator.Validate(request.Sql);
        if (!validationResult.IsValid)
        {
            return BadRequest(Result<ExecuteQueryResponse>.Fail(
                ErrorCode.SqlValidationFailed,
                validationResult.ErrorMessage ?? "SQL validation failed"));
        }

        // Get connection
        var connection = await _connectionService.GetByIdAsync(request.ConnectionId, cancellationToken);
        if (connection == null)
        {
            return NotFound(Result<ExecuteQueryResponse>.Fail(
                ErrorCode.NotFound,
                $"Connection {request.ConnectionId} not found"));
        }

        // Decrypt password (in production, use secure key management)
        var password = DecryptPassword(connection.EncryptedPassword);

        var options = new QueryExecutionOptions
        {
            TimeoutSeconds = request.TimeoutSeconds,
            MaxRows = request.MaxRows,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            CancellationToken = cancellationToken
        };

        var result = await _queryExecutor.ExecuteAsync(connection, password, request.Sql, options, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Query execution failed: {Error}", result.ErrorMessage);
            return Ok(Result<ExecuteQueryResponse>.Ok(new ExecuteQueryResponse
            {
                Success = false,
                ErrorMessage = result.ErrorMessage,
                ExecutionTimeMs = result.ExecutionTimeMs
            }));
        }

        var response = new ExecuteQueryResponse
        {
            Success = true,
            ExecutionTimeMs = result.ExecutionTimeMs,
            Result = new QueryResultDto
            {
                Columns = result.Result?.Data?.Columns.Select(c => new ColumnInfo
                {
                    Name = c.Name,
                    DataType = c.DataType,
                    IsNullable = c.IsNullable
                }).ToList() ?? new List<ColumnInfo>(),
                Rows = result.Result?.Data?.Rows.Select(r =>
                {
                    var dict = new Dictionary<string, object?>();
                    for (int i = 0; i < (result.Result?.Data?.Columns.Count ?? 0); i++)
                    {
                        var colName = result.Result!.Data!.Columns[i].Name;
                        dict[colName] = r.Values.Count > i ? r.Values[i] : null;
                    }
                    return dict;
                }).ToList() ?? new List<Dictionary<string, object?>>(),
                RowCount = result.Result?.Data?.RowCount ?? 0,
                AffectedRows = result.Result?.AffectedRows ?? 0,
                ExecutionTimeMs = result.ExecutionTimeMs,
                IsQuery = result.Result?.IsQuery ?? true,
                Message = result.Result?.Message,
                IsTruncated = result.Result?.Data?.IsTruncated ?? false,
                TotalRowCount = result.Result?.Data?.TotalRowCount
            }
        };

        _logger.LogInformation("Query executed successfully on connection {ConnectionId}", request.ConnectionId);
        return Ok(Result<ExecuteQueryResponse>.Ok(response));
    }

    /// <summary>
    /// Get query execution plan (EXPLAIN)
    /// </summary>
    [HttpPost("explain")]
    [ProducesResponseType(typeof(Result<ExplainQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ExplainQueryResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ExplainQueryResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Explain([FromBody] ExplainQueryRequest request, CancellationToken cancellationToken)
    {
        if (request.ConnectionId <= 0 || string.IsNullOrEmpty(request.Sql))
        {
            return BadRequest(Result<ExplainQueryResponse>.Fail(
                ErrorCode.ValidationFailed,
                "ConnectionId and Sql are required"));
        }

        var connection = await _connectionService.GetByIdAsync(request.ConnectionId, cancellationToken);
        if (connection == null)
        {
            return NotFound(Result<ExplainQueryResponse>.Fail(
                ErrorCode.NotFound,
                $"Connection {request.ConnectionId} not found"));
        }

        var password = DecryptPassword(connection.EncryptedPassword);
        var queryPlan = await _queryExecutor.GetQueryPlanAsync(connection, password, request.Sql, cancellationToken);

        if (queryPlan == null)
        {
            return Ok(Result<ExplainQueryResponse>.Ok(new ExplainQueryResponse
            {
                Success = false,
                ErrorMessage = "Failed to get query plan"
            }));
        }

        return Ok(Result<ExplainQueryResponse>.Ok(new ExplainQueryResponse
        {
            Success = true,
            QueryPlan = queryPlan
        }));
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
