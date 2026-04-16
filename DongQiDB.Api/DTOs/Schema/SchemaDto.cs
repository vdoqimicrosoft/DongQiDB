namespace DongQiDB.Api.DTOs.Schema;

/// <summary>
/// Column information DTO
/// </summary>
public class ColumnDto
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public string? DefaultValue { get; set; }
    public string? Comment { get; set; }
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
}

/// <summary>
/// Index information DTO
/// </summary>
public class IndexDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsUnique { get; set; }
    public bool IsPrimaryKey { get; set; }
    public List<string> Columns { get; set; } = new();
}

/// <summary>
/// Table information DTO
/// </summary>
public class TableDto
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? TableComment { get; set; }
    public long RowCount { get; set; }
    public List<ColumnDto> Columns { get; set; } = new();
    public List<IndexDto> Indexes { get; set; } = new();
}

/// <summary>
/// Schema overview DTO
/// </summary>
public class SchemaOverviewDto
{
    public long ConnectionId { get; set; }
    public List<TableDto> Tables { get; set; } = new();
}

/// <summary>
/// Table list item DTO
/// </summary>
public class TableListItemDto
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? TableComment { get; set; }
    public long RowCount { get; set; }
}
