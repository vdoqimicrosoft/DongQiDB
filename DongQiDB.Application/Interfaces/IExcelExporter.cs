using System.Data;

namespace DongQiDB.Application.Interfaces;

/// <summary>
/// Excel export service interface using ClosedXML
/// </summary>
public interface IExcelExporter
{
    /// <summary>
    /// Export DataTable to Excel bytes
    /// </summary>
    Task<byte[]> ExportToExcelAsync(DataTable data, ExcelExportOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export DataTable to Excel file
    /// </summary>
    Task<string> ExportToExcelFileAsync(DataTable data, ExcelExportOptions options, string outputPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Excel export options
/// </summary>
public class ExcelExportOptions
{
    /// <summary>
    /// Sheet name (default: Sheet1)
    /// </summary>
    public string SheetName { get; set; } = "Sheet1";

    /// <summary>
    /// Freeze top row (header)
    /// </summary>
    public bool FreezeHeader { get; set; } = true;

    /// <summary>
    /// Auto-fit column width
    /// </summary>
    public bool AutoFitColumns { get; set; } = true;

    /// <summary>
    /// Apply styling
    /// </summary>
    public bool ApplyStyle { get; set; } = true;

    /// <summary>
    /// Header background color (hex)
    /// </summary>
    public string HeaderBackgroundColor { get; set; } = "4472C4";

    /// <summary>
    /// Header font color (hex)
    /// </summary>
    public string HeaderFontColor { get; set; } = "FFFFFF";

    /// <summary>
    /// Alternate row color (hex)
    /// </summary>
    public string AlternateRowColor { get; set; } = "F2F2F2";
}
