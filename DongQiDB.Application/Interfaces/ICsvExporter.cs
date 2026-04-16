using System.Data;

namespace DongQiDB.Application.Interfaces;

/// <summary>
/// CSV export service interface
/// </summary>
public interface ICsvExporter
{
    /// <summary>
    /// Export DataTable to CSV bytes with optional UTF-8 BOM
    /// </summary>
    Task<byte[]> ExportToCsvAsync(DataTable data, CsvExportOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export DataTable to CSV file with chunking for large datasets
    /// </summary>
    Task<string> ExportToCsvFileAsync(DataTable data, CsvExportOptions options, string outputPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// CSV export options
/// </summary>
public class CsvExportOptions
{
    /// <summary>
    /// Use UTF-8 BOM for Excel compatibility
    /// </summary>
    public bool UseBom { get; set; } = true;

    /// <summary>
    /// Custom delimiter (default: comma)
    /// </summary>
    public string Delimiter { get; set; } = ",";

    /// <summary>
    /// Include header row
    /// </summary>
    public bool IncludeHeader { get; set; } = true;

    /// <summary>
    /// Chunk size for large file export
    /// </summary>
    public int ChunkSize { get; set; } = 10000;

    /// <summary>
    /// Custom date format
    /// </summary>
    public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
}
