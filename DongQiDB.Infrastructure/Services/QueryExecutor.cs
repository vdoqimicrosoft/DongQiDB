using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;
using DongQiDB.Application.DTOs;
using DongQiDB.Application.Interfaces;
using DongQiDB.Domain.Common;
using DongQiDB.Domain.Entities;
using DongQiDB.Infrastructure.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DongQiDB.Infrastructure.Services;

/// <summary>
/// Query executor implementation
/// </summary>
public class QueryExecutor : IQueryExecutor
{
    private readonly AppSettings _appSettings;
    private readonly ILogger<QueryExecutor> _logger;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runningQueries = new();

    public bool SupportsCancellation => true;

    public QueryExecutor(AppSettings appSettings, ILogger<QueryExecutor> logger)
    {
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task<QueryExecutionResult> ExecuteAsync(
        Connection connection,
        string decryptedPassword,
        string sql,
        QueryExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new QueryExecutionOptions();
        var executionId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Register for cancellation
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _runningQueries[executionId] = linkedCts;

            // Set timeout
            if (options.TimeoutSeconds > 0)
            {
                linkedCts.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
            }

            var dbConnection = CreateConnection(connection, decryptedPassword);
            await dbConnection.OpenAsync(linkedCts.Token);

            try
            {
                var isQuery = IsSelectQuery(sql);

                if (isQuery)
                {
                    var result = await ExecuteQueryAsync(
                        dbConnection, sql, options, linkedCts.Token);
                    stopwatch.Stop();

                    string? queryPlan = null;
                    if (options.GetQueryPlan)
                    {
                        queryPlan = await GetQueryPlanInternalAsync(dbConnection, sql);
                    }

                    return QueryExecutionResult.Ok(result, stopwatch.ElapsedMilliseconds, queryPlan);
                }
                else
                {
                    var affectedRows = await ExecuteNonQueryAsync(
                        dbConnection, sql, linkedCts.Token);
                    stopwatch.Stop();

                    var queryResult = new QueryResult
                    {
                        AffectedRows = affectedRows,
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                        IsQuery = false,
                        Message = $"{affectedRows} row(s) affected",
                        ExecutionId = executionId
                    };

                    return QueryExecutionResult.Ok(queryResult, stopwatch.ElapsedMilliseconds);
                }
            }
            finally
            {
                await dbConnection.CloseAsync();
                await dbConnection.DisposeAsync();
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Query execution timed out after {Timeout}s: {Sql}",
                options.TimeoutSeconds, TruncateSql(sql));
            return QueryExecutionResult.Fail("Query execution timed out");
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogInformation("Query execution was cancelled: {ExecutionId}", executionId);
            return QueryExecutionResult.Fail("Query execution was cancelled");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Query execution failed: {Sql}", TruncateSql(sql));
            return QueryExecutionResult.Fail(ex.Message);
        }
        finally
        {
            _runningQueries.TryRemove(executionId, out _);
        }
    }

    public async Task<QueryExecutionResult> ExecutePaginatedAsync(
        Connection connection,
        string decryptedPassword,
        string sql,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Check if already has LIMIT or OFFSET
        var hasLimit = sql.Contains("LIMIT", StringComparison.OrdinalIgnoreCase);
        var hasOffset = sql.Contains("OFFSET", StringComparison.OrdinalIgnoreCase);

        string paginatedSql;
        if (hasLimit || hasOffset)
        {
            // Remove existing LIMIT/OFFSET
            paginatedSql = RemoveLimitOffset(sql);
        }
        else
        {
            paginatedSql = sql;
        }

        var offset = (pageNumber - 1) * pageSize;
        paginatedSql = connection.DatabaseType switch
        {
            DatabaseType.PostgreSql => $"{paginatedSql} LIMIT {pageSize} OFFSET {offset}",
            DatabaseType.Sqlite => $"{paginatedSql} LIMIT {pageSize} OFFSET {offset}",
            _ => throw new NotSupportedException($"Database type {connection.DatabaseType} not supported")
        };

        var options = new QueryExecutionOptions
        {
            MaxRows = pageSize,
            GetQueryPlan = false
        };

        return await ExecuteAsync(connection, decryptedPassword, paginatedSql, options, cancellationToken);
    }

    public async Task<string?> GetQueryPlanAsync(
        Connection connection,
        string decryptedPassword,
        string sql,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dbConnection = CreateConnection(connection, decryptedPassword);
            await dbConnection.OpenAsync(cancellationToken);

            try
            {
                return await GetQueryPlanInternalAsync(dbConnection, sql);
            }
            finally
            {
                await dbConnection.CloseAsync();
                await dbConnection.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get query plan");
            return null;
        }
    }

    public async Task CancelAsync(Guid executionId)
    {
        if (_runningQueries.TryGetValue(executionId, out var cts))
        {
            _logger.LogInformation("Cancelling query execution: {ExecutionId}", executionId);
            await cts.CancelAsync();
        }
    }

    private static bool IsSelectQuery(string sql)
    {
        var trimmed = sql.TrimStart();
        return trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("SHOW", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("DESCRIBE", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("EXPLAIN", StringComparison.OrdinalIgnoreCase);
    }

    private static DbConnection CreateConnection(Connection connection, string decryptedPassword)
    {
        var connectionString = connection.DatabaseType switch
        {
            DatabaseType.PostgreSql => GetPostgreSqlConnectionString(connection, decryptedPassword),
            DatabaseType.Sqlite => $"Data Source={connection.Database};",
            _ => throw new NotSupportedException($"Database type {connection.DatabaseType} not supported")
        };

        return connection.DatabaseType switch
        {
            DatabaseType.PostgreSql => new NpgsqlConnection(connectionString),
            DatabaseType.Sqlite => new SqliteConnection(connectionString),
            _ => throw new NotSupportedException($"Database type {connection.DatabaseType} not supported")
        };
    }

    private static string GetPostgreSqlConnectionString(Connection connection, string decryptedPassword)
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host = connection.Host,
            Port = connection.Port,
            Database = connection.Database,
            Username = connection.Username,
            Password = decryptedPassword,
            Timeout = 30,
            CommandTimeout = 30
        }.ConnectionString;
    }

    private async Task<QueryResult> ExecuteQueryAsync(
        DbConnection connection,
        string sql,
        QueryExecutionOptions options,
        CancellationToken cancellationToken)
    {
        var resultSet = new ResultSet();
        var stopwatch = Stopwatch.StartNew();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = options.TimeoutSeconds;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        // Get columns
        var schemaTable = reader.GetColumnSchema();
        var ordinal = 0;
        foreach (var column in schemaTable)
        {
            resultSet.Columns.Add(new ResultColumn
            {
                Name = column.ColumnName ?? $"Column{ordinal}",
                DataType = column.DataType?.Name ?? "unknown",
                TypeCode = GetTypeCode(column.DataType),
                IsNullable = column.AllowDBNull ?? true,
                Ordinal = ordinal++
            });
        }

        // Read rows with optional limit
        var maxRows = options.MaxRows ?? int.MaxValue;
        var rowCount = 0;
        var totalRowCount = 0;

        while (await reader.ReadAsync(cancellationToken))
        {
            if (rowCount < maxRows)
            {
                var row = new ResultRow();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    row.Values.Add(value == DBNull.Value ? null : value);
                }
                resultSet.Rows.Add(row);
                rowCount++;
            }
            totalRowCount++;
        }

        stopwatch.Stop();
        resultSet.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        resultSet.IsTruncated = totalRowCount > maxRows;
        resultSet.TotalRowCount = totalRowCount;

        return new QueryResult
        {
            Data = resultSet,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            ExecutionId = Guid.NewGuid()
        };
    }

    private async Task<int> ExecuteNonQueryAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30;
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<string?> GetQueryPlanInternalAsync(DbConnection connection, string sql)
    {
        try
        {
            var explainSql = connection is NpgsqlConnection
                ? $"EXPLAIN (FORMAT JSON) {sql}"
                : $"EXPLAIN QUERY PLAN {sql}";

            await using var command = connection.CreateCommand();
            command.CommandText = explainSql;

            await using var reader = await command.ExecuteReaderAsync();
            var plans = new List<string>();

            while (await reader.ReadAsync())
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    if (value != DBNull.Value)
                        plans.Add(value.ToString() ?? "");
                }
            }

            return string.Join("\n", plans);
        }
        catch
        {
            return null;
        }
    }

    private static string RemoveLimitOffset(string sql)
    {
        // Remove LIMIT clause
        var result = Regex.Replace(sql, @"\bLIMIT\s+\d+", "", RegexOptions.IgnoreCase);
        // Remove OFFSET clause
        result = Regex.Replace(result, @"\bOFFSET\s+\d+", "", RegexOptions.IgnoreCase);
        return result;
    }

    private static TypeCode GetTypeCode(Type? dataType)
    {
        if (dataType == null) return TypeCode.Object;

        var typeCode = Type.GetTypeCode(dataType);
        return typeCode == TypeCode.Object ? TypeCode.String : typeCode;
    }

    private static string TruncateSql(string sql) =>
        sql.Length > 100 ? sql[..100] + "..." : sql;
}
