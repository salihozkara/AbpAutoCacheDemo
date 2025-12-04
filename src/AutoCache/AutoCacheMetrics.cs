using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AutoCache;

/// <summary>
/// Implementation of cache metrics tracking
/// </summary>
public class AutoCacheMetrics : IAutoCacheMetrics, ISingletonDependency
{
    private long _hits;
    private long _misses;
    private long _errors;
    private readonly ConcurrentDictionary<string, long> _keyHits = new();
    private readonly ConcurrentDictionary<string, long> _keyMisses = new();
    private readonly ILogger<AutoCacheMetrics> _logger;
    private readonly AutoCacheOptions _options;

    public AutoCacheMetrics(ILogger<AutoCacheMetrics> logger, IOptions<AutoCacheOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public void RecordHit(string cacheKey)
    {
        if (!_options.EnableMetrics)
        {
            return;
        }

        Interlocked.Increment(ref _hits);
        _keyHits.AddOrUpdate(cacheKey, 1, (_, count) => count + 1);

        if (_options.EnableLogging)
        {
            _logger.LogDebug("Cache HIT for key: {CacheKey}", cacheKey);
        }
    }

    public void RecordMiss(string cacheKey)
    {
        if (!_options.EnableMetrics)
        {
            return;
        }

        Interlocked.Increment(ref _misses);
        _keyMisses.AddOrUpdate(cacheKey, 1, (_, count) => count + 1);

        if (_options.EnableLogging)
        {
            _logger.LogDebug("Cache MISS for key: {CacheKey}", cacheKey);
        }
    }

    public void RecordError(string cacheKey, Exception exception)
    {
        if (!_options.EnableMetrics)
        {
            return;
        }

        Interlocked.Increment(ref _errors);

        if (_options.EnableLogging)
        {
            _logger.LogWarning(exception, "Cache ERROR for key: {CacheKey}", cacheKey);
        }
    }

    public AutoCacheStatistics GetStatistics()
    {
        var totalOperations = _hits + _misses;
        var hitRate = totalOperations > 0 ? (double)_hits / totalOperations * 100 : 0;

        return new AutoCacheStatistics
        {
            TotalHits = _hits,
            TotalMisses = _misses,
            TotalErrors = _errors,
            HitRate = hitRate,
            TotalOperations = totalOperations
        };
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _hits, 0);
        Interlocked.Exchange(ref _misses, 0);
        Interlocked.Exchange(ref _errors, 0);
        _keyHits.Clear();
        _keyMisses.Clear();
    }
}