namespace DongQiDB.Api.DTOs.Query;

/// <summary>
/// Execute query request
/// </summary>
public class ExecuteQueryRequest
{
    public long ConnectionId { get; set; }
    public string Sql { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int? MaxRows { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}

/// <summary>
/// Query execution result
/// </summary>
public class QueryResultDto
{
    public List<ColumnInfo> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int RowCount { get; set; }
    public int AffectedRows { get; set; }
    public long ExecutionTimeMs { get; set; }
    public bool IsQuery { get; set; } = true;
    public string? Message { get; set; }
    public bool IsTruncated { get; set; }
    public int? TotalRowCount { get; set; }
}

/// <summary>
/// Column information
/// </summary>
public class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
}

/// <summary>
/// Execute query response
/// </summary>
public class ExecuteQueryResponse
{
    public bool Success { get; set; }
    public QueryResultDto? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }
}

/// <summary>
/// Explain query request
/// </summary>
public class ExplainQueryRequest
{
    public long ConnectionId { get; set; }
    public string Sql { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Explain query response
/// </summary>
public class ExplainQueryResponse
{
    public bool Success { get; set; }
    public string? QueryPlan { get; set; }
    public string? ErrorMessage { get; set; }
}
