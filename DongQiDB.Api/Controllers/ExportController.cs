using System.Data;
using DongQiDB.Application.DTOs;
using DongQiDB.Application.Interfaces;
using DongQiDB.Api.DTOs.Export;
using DongQiDB.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DongQiDB.Api.Controllers;

/// <summary>
/// Export controller for CSV and Excel exports
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/export")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly ICsvExporter _csvExporter;
    private readonly IExcelExporter _excelExporter;
    private readonly IQueryExecutor _queryExecutor;
    private readonly IConnectionService _connectionService;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        ICsvExporter csvExporter,
        IExcelExporter excelExporter,
        IQueryExecutor queryExecutor,
        IConnectionService connectionService,
        IConnectionManager connectionManager,
        ILogger<ExportController> logger)
    {
        _csvExporter = csvExporter;
        _excelExporter = excelExporter;
        _queryExecutor = queryExecutor;
        _connectionService = connectionService;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    /// <summary>
    /// Export query results to CSV
    /// </summary>
    [HttpPost("csv")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(typeof(Result<object>), 400)]
    public async Task<IActionResult> ExportToCsv([FromBody] CsvExportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("CSV export requested for connection {ConnectionId}", request.ConnectionId);

            // Validate connection
            var connection = await _connectionService.GetByIdAsync(request.ConnectionId, cancellationToken);
            if (connection == null)
                return BadRequest(Result<object>.Fail(ErrorCode.NotFound, "Connection not found"));

            // Decrypt password
            var password = DecryptPassword(connection.EncryptedPassword);

            // Execute query
            var result = await _queryExecutor.ExecuteAsync(
                connection, password, request.Sql, null, cancellationToken);

            if (!result.IsSuccess || result.Result?.Data == null)
                return BadRequest(Result<object>.Fail(ErrorCode.DatabaseError, result.ErrorMessage ?? "Query execution failed"));

            // Convert to DataTable
            var dataTable = ConvertToDataTable(result.Result.Data);

            // Export to CSV
            var options = new CsvExportOptions
            {
                UseBom = request.UseBom,
                Delimiter = request.Delimiter,
                IncludeHeader = request.IncludeHeader,
                ChunkSize = request.ChunkSize
            };

            var csvBytes = await _csvExporter.ExportToCsvAsync(dataTable, options, cancellationToken);

            var fileName = $"export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(csvBytes, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSV export failed");
            return BadRequest(Result<object>.Fail(ErrorCode.InternalError, $"Export failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Export query results to Excel
    /// </summary>
    [HttpPost("excel")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(typeof(Result<object>), 400)]
    public async Task<IActionResult> ExportToExcel([FromBody] ExcelExportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Excel export requested for connection {ConnectionId}", request.ConnectionId);

            // Validate connection
            var connection = await _connectionService.GetByIdAsync(request.ConnectionId, cancellationToken);
            if (connection == null)
                return BadRequest(Result<object>.Fail(ErrorCode.NotFound, "Connection not found"));

            // Decrypt password
            var password = DecryptPassword(connection.EncryptedPassword);

            // Execute query
            var result = await _queryExecutor.ExecuteAsync(
                connection, password, request.Sql, null, cancellationToken);

            if (!result.IsSuccess || result.Result?.Data == null)
                return BadRequest(Result<object>.Fail(ErrorCode.DatabaseError, result.ErrorMessage ?? "Query execution failed"));

            // Convert to DataTable
            var dataTable = ConvertToDataTable(result.Result.Data);

            // Export to Excel
            var options = new ExcelExportOptions
            {
                SheetName = request.SheetName,
                FreezeHeader = request.FreezeHeader,
                AutoFitColumns = request.AutoFitColumns,
                ApplyStyle = request.ApplyStyle
            };

            var excelBytes = await _excelExporter.ExportToExcelAsync(dataTable, options, cancellationToken);

            var fileName = $"export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel export failed");
            return BadRequest(Result<object>.Fail(ErrorCode.InternalError, $"Export failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Convert ResultSet to DataTable for export
    /// </summary>
    private static DataTable ConvertToDataTable(ResultSet resultSet)
    {
        var dataTable = new DataTable();

        // Add columns
        foreach (var col in resultSet.Columns)
        {
            var type = col.TypeCode switch
            {
                TypeCode.Boolean => typeof(bool),
                TypeCode.DateTime => typeof(DateTime),
                TypeCode.Decimal => typeof(decimal),
                TypeCode.Double => typeof(double),
                TypeCode.Int16 => typeof(short),
                TypeCode.Int32 => typeof(int),
                TypeCode.Int64 => typeof(long),
                TypeCode.String => typeof(string),
                TypeCode.Byte => typeof(byte),
                _ => typeof(string)
            };
            dataTable.Columns.Add(col.Name, type);
        }

        // Add rows
        foreach (var row in resultSet.Rows)
        {
            var dataRow = dataTable.NewRow();
            for (int i = 0; i < resultSet.Columns.Count; i++)
            {
                dataRow[i] = row.Values.Count > i ? row.Values[i] ?? DBNull.Value : DBNull.Value;
            }
            dataTable.Rows.Add(dataRow);
        }

        return dataTable;
    }

    private static string GetEncryptionKey() => "DongQiDB32ByteEncryptionKey123456";
    private static string GetEncryptionIv() => "DongQiDB16Byte";

    private string DecryptPassword(string encryptedPassword)
    {
        try
        {
            return _connectionManager.DecryptPassword(encryptedPassword, GetEncryptionKey(), GetEncryptionIv());
        }
        catch
        {
            return string.Empty;
        }
    }
}
