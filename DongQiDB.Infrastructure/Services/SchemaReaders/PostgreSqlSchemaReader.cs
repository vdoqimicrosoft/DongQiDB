using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Npgsql;
using DongQiDB.Domain.Common;
using DongQiDB.Domain.Entities;

namespace DongQiDB.Infrastructure.Services.SchemaReaders;

/// <summary>
/// PostgreSQL schema reader
/// </summary>
public class PostgreSqlSchemaReader
{
    private readonly ILogger<PostgreSqlSchemaReader> _logger;

    public PostgreSqlSchemaReader(ILogger<PostgreSqlSchemaReader> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<TableInfo>> GetTablesAsync(
        string connectionString,
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        var tables = new List<TableInfo>();
        var schemaFilter = string.IsNullOrEmpty(schemaName) ? "" : $"AND n.nspname = '{schemaName}'";

        var sql = $@"
            SELECT
                n.nspname AS schema_name,
                c.relname AS table_name,
                obj_description(c.oid) AS table_comment,
                COALESCE(c.reltuples, 0)::bigint AS row_count
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relkind = 'r'
                AND n.nspname NOT IN ('pg_catalog', 'information_schema')
                {schemaFilter}
            ORDER BY n.nspname, c.relname";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(new TableInfo
            {
                SchemaName = reader.GetString(0),
                TableName = reader.GetString(1),
                TableComment = reader.IsDBNull(2) ? null : reader.GetString(2),
                RowCount = reader.GetInt64(3)
            });
        }

        _logger.LogInformation("Retrieved {Count} tables from PostgreSQL", tables.Count);
        return tables;
    }

    public async Task<IEnumerable<ColumnInfo>> GetColumnsAsync(
        string connectionString,
        string tableName,
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        var columns = new List<ColumnInfo>();
        var schemaFilter = string.IsNullOrEmpty(schemaName) ? "" : $"AND n.nspname = '{schemaName}'";

        var sql = $@"
            SELECT
                a.attname AS column_name,
                pg_catalog.format_type(a.atttypid, a.atttypmod) AS column_type,
                CASE WHEN a.attlen > 0 THEN a.attlen ELSE NULL END AS max_length,
                CASE WHEN a.atttypmod > 4 THEN (a.atttypmod - 4) ELSE NULL END AS precision_,
                CASE WHEN a.atttypmod > 4 THEN NULL ELSE NULL END AS scale,
                NOT a.attnotnull AS is_nullable,
                EXISTS (
                    SELECT 1 FROM pg_constraint cs
                    JOIN pg_attribute ca ON ca.attrelid = cs.conrelid AND ca.attnum = ANY(cs.conkey)
                    WHERE cs.conrelid = a.attrelid AND cs.contype = 'p' AND ca.attnum = a.attnum
                ) AS is_primary_key,
                FALSE AS is_foreign_key,
                a.attidentity IN ('a', 'd') AS is_auto_increment,
                col_description(a.attrelid, a.attnum) AS column_comment,
                a.attnum AS ordinal_position
            FROM pg_attribute a
            JOIN pg_class c ON c.oid = a.attrelid
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE a.attnum > 0
                AND NOT a.attisdropped
                AND c.relname = '{tableName}'
                {schemaFilter}
            ORDER BY a.attnum";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnInfo
            {
                ColumnName = reader.GetString(0),
                ColumnType = reader.IsDBNull(1) ? null : reader.GetString(1),
                MaxLength = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Precision = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Scale = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                IsNullable = reader.GetBoolean(5),
                IsPrimaryKey = reader.GetBoolean(6),
                IsForeignKey = reader.GetBoolean(7),
                IsAutoIncrement = reader.GetBoolean(8),
                ColumnComment = reader.IsDBNull(9) ? null : reader.GetString(9),
                OrdinalPosition = reader.GetInt32(10)
            });
        }

        _logger.LogInformation("Retrieved {Count} columns for table {TableName}", columns.Count, tableName);
        return columns;
    }

    public async Task<IEnumerable<IndexInfo>> GetIndexesAsync(
        string connectionString,
        string tableName,
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        var indexes = new List<IndexInfo>();
        var schemaFilter = string.IsNullOrEmpty(schemaName) ? "" : $"AND n.nspname = '{schemaName}'";

        var sql = $@"
            SELECT
                i.relname AS index_name,
                ix.indisunique AS is_unique,
                ix.indisprimary AS is_primary_key,
                am.amname AS index_type,
                pg_get_expr(ix.indpred, ix.indrelid) AS filter_condition,
                ARRAY_TO_STRING(
                    ARRAY(
                        SELECT attname
                        FROM UNNEST(ix.indkey) WITH ORDINALITY AS t(attnum, ord)
                        JOIN pg_attribute a ON a.attrelid = ix.indrelid AND a.attnum = t.attnum
                        ORDER BY t.ord
                    ), ','
                ) AS columns
            FROM pg_index ix
            JOIN pg_class i ON i.oid = ix.indexrelid
            JOIN pg_class t ON t.oid = ix.indrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            JOIN pg_am am ON am.oid = i.relam
            WHERE t.relname = '{tableName}'
                AND NOT ix.indisprimary
                {schemaFilter}
            ORDER BY i.relname";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            indexes.Add(new IndexInfo
            {
                IndexName = reader.GetString(0),
                IsUnique = reader.GetBoolean(1),
                IsPrimaryKey = reader.GetBoolean(2),
                IndexType = reader.IsDBNull(3) ? null : reader.GetString(3),
                FilterCondition = reader.IsDBNull(4) ? null : reader.GetString(4),
                Columns = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        _logger.LogInformation("Retrieved {Count} indexes for table {TableName}", indexes.Count, tableName);
        return indexes;
    }
}
