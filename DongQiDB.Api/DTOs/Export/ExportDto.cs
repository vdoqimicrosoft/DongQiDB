using System.ComponentModel.DataAnnotations;

namespace DongQiDB.Api.DTOs.Export;

/// <summary>
/// Base export request
/// </summary>
public class ExportRequest
{
    /// <summary>
    /// Connection ID
    /// </summary>
    [Required]
    public long ConnectionId { get; set; }

    /// <summary>
    /// SQL query to export
    /// </summary>
    [Required]
    public string Sql { get; set; } = string.Empty;
}

/// <summary>
/// CSV export request
/// </summary>
public class CsvExportRequest : ExportRequest
{
    /// <summary>
    /// Use UTF-8 BOM
    /// </summary>
    public bool UseBom { get; set; } = true;

    /// <summary>
    /// Custom delimiter
    /// </summary>
    public string Delimiter { get; set; } = ",";

    /// <summary>
    /// Include header
    /// </summary>
    public bool IncludeHeader { get; set; } = true;

    /// <summary>
    /// Chunk size for large exports
    /// </summary>
    public int ChunkSize { get; set; } = 10000;
}

/// <summary>
/// Excel export request
/// </summary>
public class ExcelExportRequest : ExportRequest
{
    /// <summary>
    /// Sheet name
    /// </summary>
    public string SheetName { get; set; } = "Sheet1";

    /// <summary>
    /// Freeze header row
    /// </summary>
    public bool FreezeHeader { get; set; } = true;

    /// <summary>
    /// Auto-fit columns
    /// </summary>
    public bool AutoFitColumns { get; set; } = true;

    /// <summary>
    /// Apply styling
    /// </summary>
    public bool ApplyStyle { get; set; } = true;
}

/// <summary>
/// Export response
/// </summary>
public class ExportResponse
{
    /// <summary>
    /// Success flag
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Row count
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? Error { get; set; }
}
