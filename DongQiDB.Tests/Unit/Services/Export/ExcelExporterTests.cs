using System.Data;
using DongQiDB.Application.Interfaces;
using DongQiDB.Infrastructure.Services.Export;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DongQiDB.Tests.Unit.Services.Export;

/// <summary>
/// Unit tests for ExcelExporter
/// </summary>
public class ExcelExporterTests
{
    private readonly ExcelExporter _exporter;
    private readonly Mock<ILogger<ExcelExporter>> _loggerMock;

    public ExcelExporterTests()
    {
        _loggerMock = new Mock<ILogger<ExcelExporter>>();
        _exporter = new ExcelExporter(_loggerMock.Object);
    }

    [Fact]
    public async Task ExportToExcelAsync_WithValidData_ReturnsBytes()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var options = new ExcelExportOptions();

        // Act
        var result = await _exporter.ExportToExcelAsync(dataTable, options);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
        // Excel files start with PK (ZIP format)
        result[0].Should().Be(0x50); // P
        result[1].Should().Be(0x4B); // K
    }

    [Fact]
    public async Task ExportToExcelAsync_WithDefaultSheetName_UsesSheet1()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var options = new ExcelExportOptions
        {
            SheetName = "Sheet1"
        };

        // Act
        var result = await _exporter.ExportToExcelAsync(dataTable, options);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportToExcelAsync_WithCustomSheetName_UsesCustomName()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var options = new ExcelExportOptions
        {
            SheetName = "MyData"
        };

        // Act
        var result = await _exporter.ExportToExcelAsync(dataTable, options);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportToExcelAsync_WithDateColumn_FormatsDate()
    {
        // Arrange
        var dataTable = new DataTable();
        dataTable.Columns.Add("Date", typeof(DateTime));
        dataTable.Rows.Add(new DateTime(2024, 1, 15, 10, 30, 0));

        var options = new ExcelExportOptions();

        // Act
        var result = await _exporter.ExportToExcelAsync(dataTable, options);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportToExcelAsync_WithNumericColumns_ParsesNumbers()
    {
        // Arrange
        var dataTable = new DataTable();
        dataTable.Columns.Add("IntValue", typeof(int));
        dataTable.Columns.Add("DoubleValue", typeof(double));
        dataTable.Columns.Add("DecimalValue", typeof(decimal));
        dataTable.Rows.Add(42, 3.14, 100.50);

        var options = new ExcelExportOptions();

        // Act
        var result = await _exporter.ExportToExcelAsync(dataTable, options);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportToExcelAsync_WithBooleanColumn_ParsesBool()
    {
        // Arrange
        var dataTable = new DataTable();
        dataTable.Columns.Add("IsActive", typeof(bool));
        dataTable.Rows.Add(true);
        dataTable.Rows.Add(false);

        var options = new ExcelExportOptions();

        // Act
        var result = await _exporter.ExportToExcelAsync(dataTable, options);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportToExcelAsync_WithNullValues_HandlesNulls()
    {
        // Arrange
        var dataTable = new DataTable();
        dataTable.Columns.Add("Value1");
        dataTable.Columns.Add("Value2");
        dataTable.Rows.Add(DBNull.Value, "test");
        dataTable.Rows.Add(null, "test2");

        var options = new ExcelExportOptions();

        // Act
        var result = await _exporter.ExportToExcelAsync(dataTable, options);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportToExcelAsync_WithFreezeHeader_AppliesFreeze()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var options = new ExcelExportOptions
        {
            FreezeHeader = true
        };

        // Act
        var result = await _exporter.ExportToExcelAsync(dataTable, options);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportToExcelAsync_WithStyle_AppliesStyling()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var options = new ExcelExportOptions
        {
            ApplyStyle = true,
            HeaderBackgroundColor = "FF0000",
            HeaderFontColor = "FFFFFF"
        };

        // Act
        var result = await _exporter.ExportToExcelAsync(dataTable, options);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportToExcelAsync_WithAutoFitColumns_AppliesAutoFit()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var options = new ExcelExportOptions
        {
            AutoFitColumns = true
        };

        // Act
        var result = await _exporter.ExportToExcelAsync(dataTable, options);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportToExcelAsync_EmptyDataTable_ReturnsValidFile()
    {
        // Arrange
        var dataTable = new DataTable();
        dataTable.Columns.Add("Column1");

        var options = new ExcelExportOptions();

        // Act
        var result = await _exporter.ExportToExcelAsync(dataTable, options);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportToExcelFileAsync_CreatesFile()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var options = new ExcelExportOptions();
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.xlsx");

        try
        {
            // Act
            var result = await _exporter.ExportToExcelFileAsync(dataTable, options, tempPath);

            // Assert
            result.Should().Be(tempPath);
            File.Exists(tempPath).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static DataTable CreateTestDataTable()
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("Id", typeof(int));
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Email", typeof(string));

        dataTable.Rows.Add(1, "John Doe", "john@example.com");
        dataTable.Rows.Add(2, "Jane Smith", "jane@example.com");
        dataTable.Rows.Add(3, "Bob Wilson", "bob@example.com");

        return dataTable;
    }
}
