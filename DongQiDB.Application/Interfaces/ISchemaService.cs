using DongQiDB.Domain.Entities;

namespace DongQiDB.Application.Interfaces;

/// <summary>
/// Schema service interface
/// </summary>
public interface ISchemaService
{
    /// <summary>
    /// Gets all tables for a connection
    /// </summary>
    Task<IEnumerable<TableInfo>> GetTablesAsync(
        long connectionId,
        string decryptedPassword,
        string? schemaName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all columns for a table
    /// </summary>
    Task<IEnumerable<ColumnInfo>> GetColumnsAsync(
        long connectionId,
        string tableName,
        string decryptedPassword,
        string? schemaName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all indexes for a table
    /// </summary>
    Task<IEnumerable<IndexInfo>> GetIndexesAsync(
        long connectionId,
        string tableName,
        string decryptedPassword,
        string? schemaName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets full schema (tables, columns, indexes) for a connection
    /// </summary>
    Task<SchemaResult> GetFullSchemaAsync(
        long connectionId,
        string decryptedPassword,
        string? schemaName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes schema cache for a connection
    /// </summary>
    Task RefreshSchemaAsync(
        long connectionId,
        string decryptedPassword,
        CancellationToken cancellationToken = default);
}

public record SchemaResult(
    IEnumerable<TableInfo> Tables,
    IEnumerable<ColumnInfo> Columns,
    IEnumerable<IndexInfo> Indexes);