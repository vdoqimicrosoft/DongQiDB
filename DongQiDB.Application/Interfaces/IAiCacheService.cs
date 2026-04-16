using DongQiDB.Application.DTOs;

namespace DongQiDB.Application.Interfaces;

/// <summary>
/// AI cache service interface
/// </summary>
public interface IAiCacheService
{
    /// <summary>
    /// Gets cached schema for a connection
    /// </summary>
    Task<string?> GetSchemaCacheAsync(long connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets schema cache for a connection
    /// </summary>
    Task SetSchemaCacheAsync(long connectionId, string schemaJson, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cached SQL result
    /// </summary>
    Task<string?> GetResultCacheAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets SQL result cache
    /// </summary>
    Task SetResultCacheAsync(string cacheKey, string resultJson, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates schema cache for a connection
    /// </summary>
    Task InvalidateSchemaCacheAsync(long connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates result cache
    /// </summary>
    Task InvalidateResultCacheAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets LRU cache entry
    /// </summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Clears all AI caches
    /// </summary>
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
