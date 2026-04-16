using DongQiDB.Domain.Common;

namespace DongQiDB.Domain.Entities;

/// <summary>
/// Column information entity
/// </summary>
public class ColumnInfo : BaseEntity
{
    public long TableId { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? ColumnType { get; set; } // e.g., varchar(255), decimal(10,2)
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsNullable { get; set; } = true;
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public bool IsAutoIncrement { get; set; }
    public string? DefaultValue { get; set; }
    public string? ColumnComment { get; set; }
    public int OrdinalPosition { get; set; }

    // Navigation properties
    public virtual TableInfo? Table { get; set; }
}
