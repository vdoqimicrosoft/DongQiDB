using DongQiDB.Application.DTOs;
using DongQiDB.Domain.Common;

namespace DongQiDB.Application.Interfaces;

/// <summary>
/// Text-to-SQL service interface
/// </summary>
public interface ITextToSqlService
{
    /// <summary>
    /// Converts natural language to SQL query
    /// </summary>
    Task<Result<TextToSqlResponse>> ConvertAsync(TextToSqlRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts natural language to SQL with streaming response
    /// </summary>
    IAsyncEnumerable<string> ConvertStreamAsync(TextToSqlRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Text-to-SQL request model
/// </summary>
public class TextToSqlRequest
{
    public long ConnectionId { get; set; }
    public string UserQuestion { get; set; } = string.Empty;
    public string? SchemaName { get; set; }
    public string? Context { get; set; } // decrypted password for schema access
    public string DatabaseType { get; set; } = "postgresql";
    public TextToSqlOptions? Options { get; set; }
}

/// <summary>
/// Text-to-SQL options
/// </summary>
public class TextToSqlOptions
{
    public bool IncludeExplanation { get; set; } = true;
    public bool ValidateOnly { get; set; } = false;
    public int MaxRetries { get; set; } = 2;
    public bool StreamOutput { get; set; } = false;
}

/// <summary>
/// Text-to-SQL response model
/// </summary>
public class TextToSqlResponse
{
    public string SqlQuery { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty;
    public List<string> TablesUsed { get; set; } = new();
    public List<string> Parameters { get; set; } = new();
    public bool IsValid { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public double Confidence { get; set; }
}
