namespace DongQiDB.Application.Interfaces;

/// <summary>
/// SQL validation result
/// </summary>
public class SqlValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> Warnings { get; init; } = new();
    public bool IsReadOnly { get; init; }

    public static SqlValidationResult Success(bool isReadOnly = true)
        => new() { IsValid = true, IsReadOnly = isReadOnly };

    public static SqlValidationResult Fail(string errorMessage)
        => new() { IsValid = false, ErrorMessage = errorMessage };

    public static SqlValidationResult SuccessWithWarnings(bool isReadOnly, List<string> warnings)
        => new() { IsValid = true, IsReadOnly = isReadOnly, Warnings = warnings };
}

/// <summary>
/// SQL validation rules configuration
/// </summary>
public class SqlValidationRules
{
    public bool AllowDrop { get; set; } = false;
    public bool AllowDelete { get; set; } = false;
    public bool AllowUpdate { get; set; } = false;
    public bool AllowInsert { get; set; } = false;
    public bool AllowAlter { get; set; } = false;
    public bool AllowCreate { get; set; } = false;
    public bool AllowExecute { get; set; } = false;
    public bool RequireSelect { get; set; } = true;
    public bool AllowMultipleStatements { get; set; } = false;
    public int MaxQueryLength { get; set; } = 10000;
}

/// <summary>
/// SQL validator interface
/// </summary>
public interface ISqlValidator
{
    /// <summary>
    /// Validates SQL query
    /// </summary>
    SqlValidationResult Validate(string sql);

    /// <summary>
    /// Checks if query is read-only (SELECT only)
    /// </summary>
    bool IsReadOnlyQuery(string sql);

    /// <summary>
    /// Validates SQL syntax
    /// </summary>
    bool ValidateSyntax(string sql);

    /// <summary>
    /// Checks for parameterized queries
    /// </summary>
    bool HasParameterizedValues(string sql);

    /// <summary>
    /// Sets validation rules
    /// </summary>
    void SetRules(SqlValidationRules rules);

    /// <summary>
    /// Gets current validation rules
    /// </summary>
    SqlValidationRules GetRules();
}
