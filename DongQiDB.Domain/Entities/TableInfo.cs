using DongQiDB.Domain.Common;

namespace DongQiDB.Domain.Entities;

/// <summary>
/// Table information entity
/// </summary>
public class TableInfo : BaseEntity
{
    public long ConnectionId { get; set; }
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? TableComment { get; set; }
    public long RowCount { get; set; }

    // Navigation properties
    public virtual Connection? Connection { get; set; }
    public virtual ICollection<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();
    public virtual ICollection<IndexInfo> Indexes { get; set; } = new List<IndexInfo>();
}
