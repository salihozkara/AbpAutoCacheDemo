using System;
using Volo.Abp.Caching;

namespace AutoCache;

/// <summary>
/// Wrapper for cached values with metadata
/// </summary>
[CacheName(AutoCacheConstants.CacheName)]
public class AutoCacheWrapper<T>
{
    /// <summary>
    /// The cached value
    /// </summary>
    public T Value { get; set; }

    /// <summary>
    /// Timestamp when the value was cached
    /// </summary>
    public DateTimeOffset CachedAt { get; set; } = DateTimeOffset.UtcNow;
}