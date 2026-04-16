namespace DongQiDB.Application.Interfaces;

/// <summary>
/// SQL to natural language explanation service interface
/// </summary>
public interface ISqlToTextService
{
    /// <summary>
    /// Explains SQL query in natural language
    /// </summary>
    Task<SqlToTextResponse> ExplainAsync(SqlToTextRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// SQL to text request model
/// </summary>
public class SqlToTextRequest
{
    public string SqlQuery { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = "postgresql";
    public bool IncludeDetails { get; set; } = true;
}

/// <summary>
/// SQL to text response model
/// </summary>
public class SqlToTextResponse
{
    public string Explanation { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> TablesInvolved { get; set; } = new();
    public List<string> Operations { get; set; } = new();
    public List<SqlConditionInfo> Conditions { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class SqlConditionInfo
{
    public string Column { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
