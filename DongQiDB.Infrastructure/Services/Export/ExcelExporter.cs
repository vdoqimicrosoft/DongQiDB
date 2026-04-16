using System.Data;
using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using DongQiDB.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DongQiDB.Infrastructure.Services.Export;

/// <summary>
/// Excel exporter implementation using ClosedXML
/// </summary>
public class ExcelExporter : IExcelExporter
{
    private readonly ILogger<ExcelExporter> _logger;

    public ExcelExporter(ILogger<ExcelExporter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportToExcelAsync(DataTable data, ExcelExportOptions options, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting {RowCount} rows to Excel", data.Rows.Count);

        using var workbook = CreateWorkbook(data, options);
        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        await Task.CompletedTask; // Keep async signature

        return memoryStream.ToArray();
    }

    /// <inheritdoc />
    public async Task<string> ExportToExcelFileAsync(DataTable data, ExcelExportOptions options, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting {RowCount} rows to Excel file: {Path}", data.Rows.Count, outputPath);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var workbook = CreateWorkbook(data, options);
        workbook.SaveAs(outputPath);
        await Task.CompletedTask;

        return outputPath;
    }

    private XLWorkbook CreateWorkbook(DataTable data, ExcelExportOptions options)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(options.SheetName);

        // Write header
        var headerRange = worksheet.Range(1, 1, 1, data.Columns.Count);
        for (int col = 0; col < data.Columns.Count; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = data.Columns[col].ColumnName;

            if (options.ApplyStyle)
            {
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml($"#{options.HeaderBackgroundColor}");
                cell.Style.Font.FontColor = XLColor.FromHtml($"#{options.HeaderFontColor}");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        // Freeze header row
        if (options.FreezeHeader)
        {
            worksheet.SheetView.FreezeRows(1);
        }

        // Write data rows
        for (int row = 0; row < data.Rows.Count; row++)
        {
            for (int col = 0; col < data.Columns.Count; col++)
            {
                var cell = worksheet.Cell(row + 2, col + 1);
                var value = data.Rows[row][col];

                if (value != null && value != DBNull.Value)
                {
                    // Format dates
                    if (value is DateTime dt)
                    {
                        cell.Value = dt;
                        cell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                    }
                    else if (value is bool b)
                    {
                        cell.Value = b;
                    }
                    else if (IsNumeric(value))
                    {
                        cell.Value = Convert.ToDouble(value);
                    }
                    else
                    {
                        cell.Value = value.ToString() ?? string.Empty;
                    }
                }

                // Alternate row coloring
                if (options.ApplyStyle && row % 2 == 1)
                {
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml($"#{options.AlternateRowColor}");
                }
            }
        }

        // Auto-fit columns
        if (options.AutoFitColumns)
        {
            worksheet.Columns().AdjustToContents();
        }

        // Set column widths for better appearance
        worksheet.Columns().Style.Alignment.WrapText = true;

        return workbook;
    }

    private static bool IsNumeric(object value)
    {
        if (value is sbyte or byte or short or ushort or int or uint or long or ulong
            or float or double or decimal)
            return true;

        if (value is string str)
            return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out _);

        return false;
    }
}
