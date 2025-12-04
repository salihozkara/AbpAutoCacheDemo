using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;

namespace AutoCache;

public class RedisAutoCacheKeyManager : IAutoCacheKeyManager, ITransientDependency
{
    private readonly AbpRedisCache _redisCache;
    private readonly static MethodInfo ConnectAsyncMethod;
    private readonly ILogger<RedisAutoCacheKeyManager> _logger;
    private readonly IDistributedCache<AutoCacheWrapper<object>> _cache;
    private readonly AutoCacheUserIdSelectorOptions _userIdSelectorOptions;
    
    static RedisAutoCacheKeyManager()
    {
        ConnectAsyncMethod = typeof(AbpRedisCache).GetMethod("ConnectAsync", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public RedisAutoCacheKeyManager(IDistributedCache redisCache, ILogger<RedisAutoCacheKeyManager> logger, IDistributedCache<AutoCacheWrapper<object>> cache, IOptions<AutoCacheUserIdSelectorOptions> userIdSelectorOptions)
    {
        _redisCache = redisCache as AbpRedisCache
                      ?? throw new ArgumentException("The redisCache must be of type AbpRedisCache", nameof(redisCache));
        _logger = logger;
        _cache = cache;
        _userIdSelectorOptions = userIdSelectorOptions.Value;
    }
    
    public async Task RemoveCacheAndCacheKeys(Type entityType, RemoveCacheKeyContext context)
    {
        try
        {
            var userIds = context.UserIds.IsNullOrEmpty() ? [null] : context.UserIds!.ToArray();
            var db = await ConnectAsync();
            var cacheKeys = Enum.GetValues<AutoCacheScope>()
                .SelectMany(scope => userIds.SelectMany(x => GetEntityCacheSetKeys(entityType, scope, x, context.Keys)))
                .Distinct()
                .ToList();
            
            var tasks = cacheKeys.Select(key => ClearCacheAsync(key, db));

            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            _logger.LogError(
                e, 
                "Error occurred while removing cache and cache keys for entity type {EntityType}", 
                entityType.FullName
            );
        }
    }

    private async Task ClearCacheAsync(RedisKey key, IDatabase db)
    {
        try
        {
            var members = await db.SetMembersAsync(key);
            if (members.Length == 0)
            {
                return;
            }

            var keys = members.Select(x => x.ToString()).ToList();

            await _cache.RemoveManyAsync(keys);

            await db.KeyDeleteAsync(key);
        }
        catch (Exception e)
        {
            _logger.LogError(
                e, 
                "Error occurred while clearing cache for key {CacheKey}", 
                key
            );
        }
    }

    public async Task AddCacheKeysAsync(Type[] entityTypes, string key, AddCacheKeyContext context)
    {
        try
        {
            var db = await ConnectAsync();

            var tasks = entityTypes.SelectMany(entityType =>
                GetEntityCacheSetKeys(entityType, context.Scope, context.UserId, context.Keys)
                    .Select(k => db.SetAddAsync(k, key)));

            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            _logger.LogError(
                e, 
                "Error occurred while adding cache keys for entity types {EntityTypes}", 
                string.Join(", ", entityTypes.Select(t => t.FullName))
            );
        }
    }

    protected virtual async ValueTask<IDatabase> ConnectAsync(CancellationToken token = default)
    {
        return await (ValueTask<IDatabase>)ConnectAsyncMethod.Invoke(_redisCache, [token])!;
    }
    
    protected virtual List<RedisKey> GetEntityCacheSetKeys(Type entityType, AutoCacheScope scope, Guid? userId, object?[]? keys)
    {
        var result = new List<RedisKey>(2);
        if (scope.HasFlag(AutoCacheScope.CurrentUser) && userId.HasValue && userId.Value != Guid.Empty && _userIdSelectorOptions.ContainsUserIdSelector(entityType))
        {
            result.Add($"AutoCacheKeys:{entityType.FullName}:{userId.Value}");
        }
        
        if (scope.HasFlag(AutoCacheScope.Entity) && keys != null)
        {
            result.Add($"AutoCacheKeys:{entityType.FullName}:PK:{keys.Where(k => k != null).Select(k => k.ToString()).JoinAsString(",")}");
        }

        if (result.Count == 0)
        {
            result.Add($"AutoCacheKeys:{entityType.FullName}");
        }
        
        return result;
    }
}