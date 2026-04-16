using DongQiDB.Domain.Common;

namespace DongQiDB.Domain.Entities;

/// <summary>
/// Index information entity
/// </summary>
public class IndexInfo : BaseEntity
{
    public long TableId { get; set; }
    public string IndexName { get; set; } = string.Empty;
    public bool IsUnique { get; set; }
    public bool IsPrimaryKey { get; set; }
    public string? IndexType { get; set; } // e.g., BTREE, HASH
    public string? FilterCondition { get; set; }
    public string? Columns { get; set; } // JSON array of column names

    // Navigation properties
    public virtual TableInfo? Table { get; set; }
}
