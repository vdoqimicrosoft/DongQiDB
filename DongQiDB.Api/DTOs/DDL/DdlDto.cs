namespace DongQiDB.Api.DTOs.DDL;

/// <summary>
/// Index information DTO
/// </summary>
public class IndexInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public bool IsUnique { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public string? ReferencedTable { get; set; }
    public string? ReferencedColumns { get; set; }
}

/// <summary>
/// List indexes request
/// </summary>
public class ListIndexesRequest
{
    public long ConnectionId { get; set; }
    public string? SchemaName { get; set; }
    public string? TableName { get; set; }
}

/// <summary>
/// Create index request
/// </summary>
public class CreateIndexRequest
{
    public long ConnectionId { get; set; }
    public string SchemaName { get; set; } = "public";
    public string TableName { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public bool IsUnique { get; set; } = false;
}

/// <summary>
/// Create index response
/// </summary>
public class CreateIndexResponse
{
    public bool Success { get; set; }
    public string? Ddl { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Delete index response
/// </summary>
public class DeleteIndexResponse
{
    public bool Success { get; set; }
    public string? Ddl { get; set; }
    public string? ErrorMessage { get; set; }
}
