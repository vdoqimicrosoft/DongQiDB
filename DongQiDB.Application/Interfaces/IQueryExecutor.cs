using DongQiDB.Application.DTOs;
using DongQiDB.Domain.Entities;

namespace DongQiDB.Application.Interfaces;

/// <summary>
/// Query execution result
/// </summary>
public class QueryExecutionResult
{
    public bool IsSuccess { get; init; }
    public QueryResult? Result { get; init; }
    public string? ErrorMessage { get; init; }
    public long ExecutionTimeMs { get; init; }
    public string? QueryPlan { get; init; }

    public static QueryExecutionResult Ok(QueryResult result, long executionTimeMs, string? queryPlan = null)
        => new() { IsSuccess = true, Result = result, ExecutionTimeMs = executionTimeMs, QueryPlan = queryPlan };

    public static QueryExecutionResult Fail(string errorMessage)
        => new() { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Query execution options
/// </summary>
public class QueryExecutionOptions
{
    public int TimeoutSeconds { get; set; } = 30;
    public int? MaxRows { get; set; }
    public int? PageSize { get; set; }
    public int? PageNumber { get; set; }
    public bool GetQueryPlan { get; set; } = false;
    public CancellationToken CancellationToken { get; set; } = default;
}

/// <summary>
/// Query executor interface
/// </summary>
public interface IQueryExecutor
{
    /// <summary>
    /// Executes a SQL query
    /// </summary>
    Task<QueryExecutionResult> ExecuteAsync(
        Connection connection,
        string decryptedPassword,
        string sql,
        QueryExecutionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes query with pagination
    /// </summary>
    Task<QueryExecutionResult> ExecutePaginatedAsync(
        Connection connection,
        string decryptedPassword,
        string sql,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets query execution plan
    /// </summary>
    Task<string?> GetQueryPlanAsync(
        Connection connection,
        string decryptedPassword,
        string sql,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running query
    /// </summary>
    Task CancelAsync(Guid executionId);

    /// <summary>
    /// Tests if query can be cancelled
    /// </summary>
    bool SupportsCancellation { get; }
}
