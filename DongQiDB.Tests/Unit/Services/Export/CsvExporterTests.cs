using System.Data;
using DongQiDB.Application.Interfaces;
using DongQiDB.Infrastructure.Services.Export;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DongQiDB.Tests.Unit.Services.Export;

/// <summary>
/// Unit tests for CsvExporter
/// </summary>
public class CsvExporterTests
{
    private readonly CsvExporter _exporter;
    private readonly Mock<ILogger<CsvExporter>> _loggerMock;

    public CsvExporterTests()
    {
        _loggerMock = new Mock<ILogger<CsvExporter>>();
        _exporter = new CsvExporter(_loggerMock.Object);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithValidData_ReturnsBytes()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var options = new CsvExportOptions
        {
            UseBom = true,
            Delimiter = ",",
            IncludeHeader = true
        };

        // Act
        var result = await _exporter.ExportToCsvAsync(dataTable, options);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithBom_HasUtf8Bom()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var options = new CsvExportOptions { UseBom = true };

        // Act
        var result = await _exporter.ExportToCsvAsync(dataTable, options);

        // Assert
        // UTF-8 BOM is 3 bytes: EF BB BF
        result.Length.Should().BeGreaterThan(3);
        result[0].Should().Be(0xEF);
        result[1].Should().Be(0xBB);
        result[2].Should().Be(0xBF);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithoutBom_NoBom()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var options = new CsvExportOptions { UseBom = false };

        // Act
        var result = await _exporter.ExportToCsvAsync(dataTable, options);
        var content = System.Text.Encoding.UTF8.GetString(result);

        // Assert - content should not start with BOM character
        content.Should().NotStartWith("\ufeff");
    }

    [Fact]
    public async Task ExportToCsvAsync_WithHeader_IncludesColumnNames()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var options = new CsvExportOptions { IncludeHeader = true };

        // Act
        var result = await _exporter.ExportToCsvAsync(dataTable, options);
        var content = System.Text.Encoding.UTF8.GetString(result);

        // Assert
        content.Should().Contain("Id");
        content.Should().Contain("Name");
    }

    [Fact]
    public async Task ExportToCsvAsync_WithoutHeader_ExcludesColumnNames()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var options = new CsvExportOptions { IncludeHeader = false };

        // Act
        var result = await _exporter.ExportToCsvAsync(dataTable, options);
        var content = System.Text.Encoding.UTF8.GetString(result);

        // Assert
        content.Should().NotContain("Id");
        content.Should().NotContain("Name");
    }

    [Fact]
    public async Task ExportToCsvAsync_WithCustomDelimiter_UsesDelimiter()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var options = new CsvExportOptions { Delimiter = ";" };

        // Act
        var result = await _exporter.ExportToCsvAsync(dataTable, options);
        var content = System.Text.Encoding.UTF8.GetString(result);

        // Assert
        content.Should().Contain(";");
    }

    [Fact]
    public async Task ExportToCsvAsync_WithDateColumn_FormatsDate()
    {
        // Arrange
        var dataTable = new DataTable();
        dataTable.Columns.Add("Date", typeof(DateTime));
        dataTable.Rows.Add(new DateTime(2024, 1, 15, 10, 30, 0));

        var options = new CsvExportOptions
        {
            DateFormat = "yyyy-MM-dd HH:mm:ss"
        };

        // Act
        var result = await _exporter.ExportToCsvAsync(dataTable, options);
        var content = System.Text.Encoding.UTF8.GetString(result);

        // Assert
        content.Should().Contain("2024-01-15 10:30:00");
    }

    [Fact]
    public async Task ExportToCsvAsync_WithNullValues_HandlesNulls()
    {
        // Arrange
        var dataTable = new DataTable();
        dataTable.Columns.Add("Value");
        var row1 = dataTable.NewRow();
        row1["Value"] = DBNull.Value;
        dataTable.Rows.Add(row1);
        var row2 = dataTable.NewRow();
        row2["Value"] = DBNull.Value;
        dataTable.Rows.Add(row2);

        var options = new CsvExportOptions();

        // Act
        var result = await _exporter.ExportToCsvAsync(dataTable, options);
        var content = System.Text.Encoding.UTF8.GetString(result);

        // Assert
        content.Should().NotContain("null");
        content.Should().NotContain("NULL");
    }

    [Fact]
    public async Task ExportToCsvAsync_EmptyDataTable_ReturnsEmpty()
    {
        // Arrange
        var dataTable = new DataTable();
        dataTable.Columns.Add("Column1");

        var options = new CsvExportOptions();

        // Act
        var result = await _exporter.ExportToCsvAsync(dataTable, options);
        var content = System.Text.Encoding.UTF8.GetString(result);

        // Assert
        content.Should().Contain("Column1");
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
