using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using DongQiDB.Domain.Entities;

namespace DongQiDB.Infrastructure.Services.SchemaReaders;

/// <summary>
/// SQLite schema reader
/// </summary>
public class SqliteSchemaReader
{
    private readonly ILogger<SqliteSchemaReader> _logger;

    public SqliteSchemaReader(ILogger<SqliteSchemaReader> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<TableInfo>> GetTablesAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var tables = new List<TableInfo>();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";

        await using var command = new SqliteCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var tableName = reader.GetString(0);
            var rowCount = await GetTableRowCountAsync(connection, tableName, cancellationToken);

            tables.Add(new TableInfo
            {
                SchemaName = "main",
                TableName = tableName,
                RowCount = rowCount
            });
        }

        _logger.LogInformation("Retrieved {Count} tables from SQLite", tables.Count);
        return tables;
    }

    public async Task<IEnumerable<ColumnInfo>> GetColumnsAsync(
        string connectionString,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        var columns = new List<ColumnInfo>();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $"PRAGMA table_info('{tableName}')";

        await using var command = new SqliteCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var columnType = reader.GetString(2);
            var isPrimaryKey = reader.GetInt32(5) == 1;

            columns.Add(new ColumnInfo
            {
                ColumnName = reader.GetString(1),
                DataType = columnType,
                ColumnType = columnType,
                IsNullable = reader.GetInt32(3) == 0,
                DefaultValue = reader.IsDBNull(4) ? null : reader.GetString(4),
                IsPrimaryKey = isPrimaryKey,
                IsAutoIncrement = isPrimaryKey && columnType.ToLower() == "integer",
                OrdinalPosition = reader.GetInt32(0)
            });
        }

        _logger.LogInformation("Retrieved {Count} columns for table {TableName}", columns.Count, tableName);
        return columns;
    }

    public async Task<IEnumerable<IndexInfo>> GetIndexesAsync(
        string connectionString,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        var indexes = new List<IndexInfo>();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $"PRAGMA index_list('{tableName}')";

        await using var command = new SqliteCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var indexName = reader.GetString(1);
            var indexColumns = await GetIndexColumnsAsync(connection, indexName, cancellationToken);

            indexes.Add(new IndexInfo
            {
                IndexName = indexName,
                IsUnique = reader.GetInt32(2) == 1,
                IsPrimaryKey = false,
                IndexType = "btree",
                Columns = string.Join(",", indexColumns)
            });
        }

        _logger.LogInformation("Retrieved {Count} indexes for table {TableName}", indexes.Count, tableName);
        return indexes;
    }

    private async Task<long> GetTableRowCountAsync(
        SqliteConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT COUNT(*) FROM \"{tableName}\"";
        await using var command = new SqliteCommand(sql, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private async Task<IEnumerable<string>> GetIndexColumnsAsync(
        SqliteConnection connection,
        string indexName,
        CancellationToken cancellationToken)
    {
        var columns = new List<string>();
        var sql = $"PRAGMA index_info('{indexName}')";

        await using var command = new SqliteCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(2));
        }

        return columns;
    }
}
