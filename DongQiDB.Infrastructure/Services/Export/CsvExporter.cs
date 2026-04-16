using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DongQiDB.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DongQiDB.Infrastructure.Services.Export;

/// <summary>
/// CSV exporter implementation
/// </summary>
public class CsvExporter : ICsvExporter
{
    private readonly ILogger<CsvExporter> _logger;

    public CsvExporter(ILogger<CsvExporter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportToCsvAsync(DataTable data, CsvExportOptions options, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting {RowCount} rows to CSV", data.Rows.Count);

        using var memoryStream = new MemoryStream();
        await WriteToStreamAsync(data, options, memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    /// <inheritdoc />
    public async Task<string> ExportToCsvFileAsync(DataTable data, CsvExportOptions options, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting {RowCount} rows to CSV file: {Path}", data.Rows.Count, outputPath);

        await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
        await WriteToStreamAsync(data, options, fileStream, cancellationToken);

        return outputPath;
    }

    private async Task WriteToStreamAsync(DataTable data, CsvExportOptions options, Stream outputStream, CancellationToken cancellationToken)
    {
        var encoding = options.UseBom ? new UTF8Encoding(true) : new UTF8Encoding(false);

        using var writer = new StreamWriter(outputStream, encoding, bufferSize: 81920, leaveOpen: true);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = options.Delimiter,
            HasHeaderRecord = options.IncludeHeader
        };

        using var csv = new CsvWriter(writer, config);

        // Write header
        if (options.IncludeHeader)
        {
            foreach (DataColumn col in data.Columns)
            {
                csv.WriteField(col.ColumnName);
            }
            await csv.NextRecordAsync();
        }

        // Write rows with chunking
        var rowCount = 0;
        foreach (DataRow row in data.Rows)
        {
            foreach (DataColumn col in data.Columns)
            {
                var value = row[col];
                var formattedValue = FormatValue(value, options.DateFormat);
                csv.WriteField(formattedValue);
            }
            await csv.NextRecordAsync();
            rowCount++;

            // Flush periodically for large datasets
            if (rowCount % options.ChunkSize == 0)
            {
                await writer.FlushAsync(cancellationToken);
                _logger.LogDebug("Flushed {RowCount} rows", rowCount);
            }
        }

        await writer.FlushAsync(cancellationToken);
        _logger.LogInformation("CSV export completed: {RowCount} rows", rowCount);
    }

    private static string FormatValue(object? value, string dateFormat)
    {
        if (value == null || value == DBNull.Value)
            return string.Empty;

        return value switch
        {
            DateTime dt => dt.ToString(dateFormat),
            DateTimeOffset dto => dto.ToString(dateFormat),
            byte[] bytes => Convert.ToBase64String(bytes),
            _ => value.ToString() ?? string.Empty
        };
    }
}
