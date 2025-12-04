using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities;

namespace AutoCache;

/// <summary>
/// Configuration options for AutoCache system
/// </summary>
public class AutoCacheOptions
{
    /// <summary>
    /// Enable or disable the AutoCache system
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Default cache duration in milliseconds when not specified in attribute
    /// </summary>
    public long DefaultAbsoluteExpirationRelativeToNow { get; set; } = 300000; // 5 minutes

    /// <summary>
    /// Default sliding expiration in milliseconds when not specified in attribute
    /// </summary>
    public long DefaultSlidingExpiration { get; set; }

    /// <summary>
    /// Enable detailed logging for cache operations
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// Enable metrics collection for cache operations (hit/miss rates, etc.)
    /// </summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>
    /// Throw exceptions on cache errors instead of falling back to method execution
    /// </summary>
    public bool ThrowOnError { get; set; } = false;

    /// <summary>
    /// Global prefix for all cache keys
    /// </summary>
    public string KeyPrefix { get; set; } = "AutoCache";

    /// <summary>
    /// Enable cache key compression for large parameter sets
    /// </summary>
    public bool EnableKeyCompression { get; set; } = true;

    /// <summary>
    /// Maximum cache key length before compression is applied
    /// </summary>
    public int MaxKeyLengthBeforeCompression { get; set; } = 250;
    
    public List<(int Order, Func<object, object?[]?> Selector)> KeyParametersSelectors { get; private set; } = 
        [(int.MaxValue, o => o is IEntity entity ? entity.GetKeys() : null)];
    
    public void AddKeyParametersSelector(int order, Func<object, object?[]?> selector)
    {
        KeyParametersSelectors.Add((order, selector));
    }
    
    public void AddKeyParametersSelector(Func<object, object?[]?> selector)
    {
        AddKeyParametersSelector(1000, selector);
    }
    
    public object?[]? GetKeyParameters(object result)
    {
        KeyParametersSelectors.Sort((a, b) => a.Order.CompareTo(b.Order));
        foreach (var (_, selector) in KeyParametersSelectors)
        {
            var keys = selector(result);
            if (keys is { Length: > 0 })
            {
                return keys;
            }
        }

        return null;
    }
}