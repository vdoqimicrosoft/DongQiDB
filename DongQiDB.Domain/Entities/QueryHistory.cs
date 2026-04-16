using DongQiDB.Domain.Common;

namespace DongQiDB.Domain.Entities;

/// <summary>
/// Query history entity
/// </summary>
public class QueryHistory : BaseEntity
{
    public long ConnectionId { get; set; }
    public string NaturalLanguageQuery { get; set; } = string.Empty;
    public string GeneratedSql { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }
    public int RowCount { get; set; }
    public string? AiModel { get; set; }
    public string? SessionId { get; set; }

    // Navigation properties
    public virtual Connection? Connection { get; set; }
}
