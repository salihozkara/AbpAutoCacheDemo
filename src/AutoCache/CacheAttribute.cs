using System;
using Volo.Abp.Domain.Entities;

namespace AutoCache;

/// <summary>
/// Attribute to mark methods for automatic caching
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class CacheAttribute : Attribute
{
    /// <summary>
    /// Entity types that affect this cache. When these entities change, the cache will be invalidated.
    /// </summary>
    public Type[] InvalidateOnEntities { get; set; }

    /// <summary>
    /// Scope of the cache (Global, CurrentUser, or AuthenticatedUser)
    /// </summary>
    public AutoCacheScope Scope { get; set; } = AutoCacheScope.Global;

    /// <summary>
    /// Absolute expiration time relative to now in milliseconds (0 = use default) (-1 = disabled)
    /// </summary>
    public long AbsoluteExpirationRelativeToNow { get; set; }

    /// <summary>
    /// Sliding expiration time in milliseconds (0 = use default) (-1 = disabled)
    /// </summary>
    public long SlidingExpiration { get; set; }

    public bool ConsiderUow { get; set; }

    public string AdditionalCacheKey { get; set; }

    /// <summary>
    /// Creates a new AutoCache attribute with specified entity types
    /// </summary>
    /// <param name="invalidateOnEntities">Entity types that affect this cache</param>
    /// <exception cref="ArgumentNullException">If any entity type is null</exception>
    /// <exception cref="ArgumentException">If any type doesn't implement IEntity</exception>
    public CacheAttribute(params Type[] invalidateOnEntities)
    {
        foreach (var entityType in invalidateOnEntities)
        {
            ArgumentNullException.ThrowIfNull(entityType);
            if (!typeof(IEntity).IsAssignableFrom(entityType))
            {
                throw new ArgumentException($"Type {entityType.FullName} must implement IEntity interface.");
            }
        }
        InvalidateOnEntities = invalidateOnEntities;
    }
}