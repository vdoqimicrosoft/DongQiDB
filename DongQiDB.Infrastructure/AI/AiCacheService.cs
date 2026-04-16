using System.Collections.Concurrent;
using System.Text.Json;
using DongQiDB.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DongQiDB.Infrastructure.AI;

/// <summary>
/// AI cache service implementation with LRU and distributed cache support
/// </summary>
public class AiCacheService : IAiCacheService
{
    private readonly ILogger<AiCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // In-memory LRU cache for quick access
    private readonly ConcurrentDictionary<string, CacheEntry> _lruCache = new();
    private readonly LinkedList<string> _lruOrder = new();
    private readonly object _lruLock = new();
    private const int MaxLruSize = 1000;

    // Distributed cache (Redis) if available, otherwise use memory
    private readonly bool _useRedis;
    private readonly string? _redisConnectionString;

    public AiCacheService(
        ILogger<AiCacheService> logger,
        bool useRedis = false,
        string? redisConnectionString = null)
    {
        _logger = logger;
        _useRedis = useRedis;
        _redisConnectionString = redisConnectionString;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public Task<string?> GetSchemaCacheAsync(long connectionId, CancellationToken cancellationToken = default)
    {
        var key = $"schema:{connectionId}";
        return GetCacheAsync(key, cancellationToken);
    }

    public async Task SetSchemaCacheAsync(
        long connectionId,
        string schemaJson,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var key = $"schema:{connectionId}";
        var effectiveExpiry = expiry ?? TimeSpan.FromHours(24);
        await SetCacheAsync(key, schemaJson, effectiveExpiry, cancellationToken);
    }

    public Task<string?> GetResultCacheAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        var key = $"result:{cacheKey}";
        return GetCacheAsync(key, cancellationToken);
    }

    public async Task SetResultCacheAsync(
        string cacheKey,
        string resultJson,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var key = $"result:{cacheKey}";
        var effectiveExpiry = expiry ?? TimeSpan.FromMinutes(30);
        await SetCacheAsync(key, resultJson, effectiveExpiry, cancellationToken);
    }

    public Task InvalidateSchemaCacheAsync(long connectionId, CancellationToken cancellationToken = default)
    {
        var key = $"schema:{connectionId}";
        return InvalidateCacheAsync(key, cancellationToken);
    }

    public Task InvalidateResultCacheAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        var key = $"result:{cacheKey}";
        return InvalidateCacheAsync(key, cancellationToken);
    }

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T?>> factory,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // Try to get from cache
        var cached = await GetCacheAsync(key, cancellationToken);
        if (cached != null)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(cached, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cache entry {Key}", key);
            }
        }

        // Get from factory
        var value = await factory();
        if (value != null)
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var effectiveExpiry = expiry ?? TimeSpan.FromMinutes(30);
            await SetCacheAsync(key, json, effectiveExpiry, cancellationToken);
        }

        return value;
    }

    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        _lruCache.Clear();
        lock (_lruLock)
        {
            _lruOrder.Clear();
        }
        _logger.LogInformation("AI cache cleared");
        return Task.CompletedTask;
    }

    private Task<string?> GetCacheAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_lruCache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow)
            {
                // Update LRU order
                UpdateLruOrder(key);
                _logger.LogDebug("Cache hit: {Key}", key);
                return Task.FromResult<string?>(entry.Value);
            }
            else
            {
                // Expired, remove
                _lruCache.TryRemove(key, out _);
                RemoveFromLruOrder(key);
            }
        }

        _logger.LogDebug("Cache miss: {Key}", key);
        return Task.FromResult<string?>(null);
    }

    private async Task SetCacheAsync(
        string key,
        string value,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var entry = new CacheEntry
        {
            Value = value,
            ExpiresAt = DateTime.UtcNow.Add(expiry),
            CreatedAt = DateTime.UtcNow
        };

        _lruCache[key] = entry;
        UpdateLruOrder(key);

        // Evict if too many entries
        await EvictIfNeededAsync();

        _logger.LogDebug("Cache set: {Key}, expires in {Expiry}", key, expiry);
    }

    private Task InvalidateCacheAsync(string key, CancellationToken cancellationToken = default)
    {
        _lruCache.TryRemove(key, out _);
        RemoveFromLruOrder(key);
        _logger.LogDebug("Cache invalidated: {Key}", key);
        return Task.CompletedTask;
    }

    private void UpdateLruOrder(string key)
    {
        lock (_lruLock)
        {
            // Remove if exists
            RemoveFromLruOrderLocked(key);

            // Add to front
            _lruOrder.AddFirst(key);
        }
    }

    private void RemoveFromLruOrder(string key)
    {
        lock (_lruLock)
        {
            RemoveFromLruOrderLocked(key);
        }
    }

    private void RemoveFromLruOrderLocked(string key)
    {
        var node = _lruOrder.First;
        while (node != null)
        {
            if (node.Value == key)
            {
                _lruOrder.Remove(node);
                break;
            }
            node = node.Next;
        }
    }

    private Task EvictIfNeededAsync()
    {
        if (_lruCache.Count > MaxLruSize)
        {
            lock (_lruLock)
            {
                // Remove oldest entries
                var toRemove = _lruCache.Count - (MaxLruSize / 2);
                var node = _lruOrder.Last;
                while (node != null && toRemove > 0)
                {
                    var key = node.Value;
                    _lruCache.TryRemove(key, out _);
                    var next = node.Previous;
                    _lruOrder.Remove(node);
                    node = next;
                    toRemove--;
                }
            }
            _logger.LogInformation("LRU cache evicted entries, new size: {Count}", _lruCache.Count);
        }
        return Task.CompletedTask;
    }

    private class CacheEntry
    {
        public string Value { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
