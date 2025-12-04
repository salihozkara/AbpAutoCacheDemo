using System;

namespace AutoCache;

/// <summary>
/// Tracks metrics for cache operations
/// </summary>
public interface IAutoCacheMetrics
{
    void RecordHit(string cacheKey);
    void RecordMiss(string cacheKey);
    void RecordError(string cacheKey, Exception exception);
    AutoCacheStatistics GetStatistics();
    void Reset();
}