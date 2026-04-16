using Microsoft.Extensions.Logging;
using DongQiDB.Application.Interfaces;
using DongQiDB.Domain.Common;
using DongQiDB.Domain.Entities;
using DongQiDB.Infrastructure.Configuration;
using DongQiDB.Infrastructure.Services.SchemaReaders;

namespace DongQiDB.Infrastructure.Services;

/// <summary>
/// Schema service implementation
/// </summary>
public class SchemaService : ISchemaService
{
    private readonly IConnectionManager _connectionManager;
    private readonly IConnectionService _connectionService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<SchemaService> _logger;
    private readonly Dictionary<string, SchemaCacheEntry> _cache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
    private readonly SqliteSchemaReader _sqliteReader;

    public SchemaService(
        IConnectionManager connectionManager,
        IConnectionService connectionService,
        AppSettings appSettings,
        ILogger<SchemaService> logger,
        SqliteSchemaReader sqliteReader)
    {
        _connectionManager = connectionManager;
        _connectionService = connectionService;
        _appSettings = appSettings;
        _logger = logger;
        _sqliteReader = sqliteReader;
    }

    public Task<IEnumerable<TableInfo>> GetTablesAsync(
        long connectionId,
        string decryptedPassword,
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(connectionId, schemaName);
        if (TryGetFromCache(cacheKey, out var cached))
        {
            _logger.LogDebug("Returning cached tables for connection {ConnectionId}", connectionId);
            return Task.FromResult(cached!.Tables);
        }

        // This would need the actual connection from the repository
        // For now, return empty - will be implemented with connection access
        return Task.FromResult<IEnumerable<TableInfo>>(Enumerable.Empty<TableInfo>());
    }

    public Task<IEnumerable<ColumnInfo>> GetColumnsAsync(
        long connectionId,
        string tableName,
        string decryptedPassword,
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(connectionId, schemaName);
        if (TryGetFromCache(cacheKey, out var cached))
        {
            return Task.FromResult(cached!.Columns.Where(c => c.Table?.TableName == tableName));
        }

        return Task.FromResult<IEnumerable<ColumnInfo>>(Enumerable.Empty<ColumnInfo>());
    }

    public Task<IEnumerable<IndexInfo>> GetIndexesAsync(
        long connectionId,
        string tableName,
        string decryptedPassword,
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(connectionId, schemaName);
        if (TryGetFromCache(cacheKey, out var cached))
        {
            return Task.FromResult(cached!.Indexes.Where(i => i.Table?.TableName == tableName));
        }

        return Task.FromResult<IEnumerable<IndexInfo>>(Enumerable.Empty<IndexInfo>());
    }

    public async Task<SchemaResult> GetFullSchemaAsync(
        long connectionId,
        string decryptedPassword,
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(connectionId, schemaName);
        if (TryGetFromCache(cacheKey, out var cached))
        {
            return new SchemaResult(cached!.Tables, cached.Columns, cached.Indexes);
        }

        try
        {
            // Get connection info from service
            var connectionInfo = await _connectionService.GetByIdAsync(connectionId, cancellationToken);
            if (connectionInfo == null)
            {
                return new SchemaResult(Enumerable.Empty<TableInfo>(), Enumerable.Empty<ColumnInfo>(), Enumerable.Empty<IndexInfo>());
            }

            var connectionString = _connectionManager.GetConnectionString(connectionInfo, decryptedPassword);

            // Read schema based on database type
            if (connectionInfo.DatabaseType == DatabaseType.Sqlite)
            {
                var tables = await _sqliteReader.GetTablesAsync(connectionString, cancellationToken);
                var tableList = tables.ToList();

                var allColumns = new List<ColumnInfo>();
                var allIndexes = new List<IndexInfo>();

                foreach (var table in tableList)
                {
                    var columns = await _sqliteReader.GetColumnsAsync(connectionString, table.TableName, cancellationToken);
                    foreach (var col in columns)
                    {
                        col.TableId = table.Id;
                        col.Table = table;
                    }
                    allColumns.AddRange(columns);

                    var indexes = await _sqliteReader.GetIndexesAsync(connectionString, table.TableName, cancellationToken);
                    foreach (var idx in indexes)
                    {
                        idx.TableId = table.Id;
                        idx.Table = table;
                    }
                    allIndexes.AddRange(indexes);
                }

                var result = new SchemaResult(tableList, allColumns, allIndexes);
                _cache[cacheKey] = new SchemaCacheEntry(tableList, allColumns, allIndexes, DateTime.UtcNow.Add(_cacheExpiration));
                _logger.LogInformation("Schema loaded for connection {ConnectionId}: {TableCount} tables", connectionId, tableList.Count);
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading schema for connection {ConnectionId}", connectionId);
        }

        return new SchemaResult(Enumerable.Empty<TableInfo>(), Enumerable.Empty<ColumnInfo>(), Enumerable.Empty<IndexInfo>());
    }

    public Task RefreshSchemaAsync(
        long connectionId,
        string decryptedPassword,
        CancellationToken cancellationToken = default)
    {
        // Clear cache entries for this connection
        var prefix = $"{connectionId}_";
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }

        _logger.LogInformation("Schema cache cleared for connection {ConnectionId}", connectionId);
        return Task.CompletedTask;
    }

    private string GetCacheKey(long connectionId, string? schemaName)
        => $"{connectionId}_{schemaName ?? "default"}";

    private bool TryGetFromCache(string cacheKey, out SchemaCacheEntry? entry)
    {
        if (_cache.TryGetValue(cacheKey, out entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            return true;
        }

        entry = null;
        if (_cache.ContainsKey(cacheKey))
        {
            _cache.Remove(cacheKey);
        }
        return false;
    }

    public void UpdateCache(long connectionId, string? schemaName, SchemaResult result)
    {
        var cacheKey = GetCacheKey(connectionId, schemaName);
        _cache[cacheKey] = new SchemaCacheEntry(
            result.Tables,
            result.Columns,
            result.Indexes,
            DateTime.UtcNow.Add(_cacheExpiration));

        _logger.LogDebug("Schema cache updated for connection {ConnectionId}", connectionId);
    }

    private record SchemaCacheEntry(
        IEnumerable<TableInfo> Tables,
        IEnumerable<ColumnInfo> Columns,
        IEnumerable<IndexInfo> Indexes,
        DateTime ExpiresAt);
}
