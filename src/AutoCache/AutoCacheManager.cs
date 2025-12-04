using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.DynamicProxy;
using Volo.Abp.Users;
using System.Text.Json;

namespace AutoCache;

/// <summary>
/// Manages automatic caching operations with expression-based API
/// </summary>
public class AutoCacheManager : IScopedDependency
{
    private readonly IAutoCacheKeyManager _autoCacheKeyManager;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AutoCacheManager> _logger;
    private readonly IAutoCacheMetrics _metrics;
    private readonly AutoCacheOptions _options;
    private readonly ConcurrentDictionary<string, object?> _memoryCache = new();
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public AutoCacheManager(
        IAutoCacheKeyManager autoCacheKeyManager, 
        ICurrentUser currentUser,
        ILogger<AutoCacheManager> logger,
        IAutoCacheMetrics metrics,
        IOptions<AutoCacheOptions> options, 
        IServiceScopeFactory serviceScopeFactory)
    {
        _autoCacheKeyManager = autoCacheKeyManager;
        _currentUser = currentUser;
        _logger = logger;
        _metrics = metrics;
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
    }

    public async Task<TResult> GetOrAddAsync<TResult>(
        object? caller,
        Func<Task<TResult>> func,
        object?[]? parameters = null,
        Func<DistributedCacheEntryOptions>? optionsFactory = null,
        Type[]? invalidateOnEntities = null,
        AutoCacheScope scope = AutoCacheScope.Global,
        bool considerUow = false,
        string? additionalCacheKey = null,
        [CallerMemberName] string methodName = "")
    {
        if (!_options.Enabled)
        {
            return await func();
        }
        
        var callerType = caller != null ? ProxyHelper.GetUnProxiedType(caller) : GetType();
        parameters ??= [];
        
        var cacheKey = GenerateCacheKey<TResult>(callerType.Name, additionalCacheKey, methodName, parameters, scope);
        
        var (cachedResult, exception, wasHit) = await GetOrAddCacheAsync(
            cacheKey,
            func,
            optionsFactory,
            considerUow
        );
        
        if (wasHit)
        {
            _metrics.RecordHit(cacheKey);
        }
        else
        {
            _metrics.RecordMiss(cacheKey);
        }
        
        if (exception != null)
        {
            _metrics.RecordError(cacheKey, exception);
            
            if (exception is AutoCacheExceptionWrapper exceptionWrapper)
            {
                if (_options.ThrowOnError)
                {
                    throw exceptionWrapper.OriginalException;
                }
                _logger.LogWarning(
                    exceptionWrapper.OriginalException,
                    "Cache operation failed for key {CacheKey}, falling back to method execution",
                    cacheKey
                );
            }
            else if (_options.ThrowOnError)
            {
                throw exception;
            }
        }
        
        if (invalidateOnEntities is { Length: > 0 } && !wasHit)
        {
            await _autoCacheKeyManager.AddCacheKeysAsync(invalidateOnEntities, cacheKey, new AddCacheKeyContext()
            {
                Scope = scope,
                UserId = _currentUser.Id,
                Keys = cachedResult != null ? _options.GetKeyParameters(cachedResult) : null
            });
        }
        
        return cachedResult!;
    }

    private async Task<(TResult?, Exception?, bool)> GetOrAddCacheAsync<TResult>(
        string cacheKey, 
        Func<Task<TResult>> func,
        Func<DistributedCacheEntryOptions>? optionsFactory,
        bool considerUow)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache<AutoCacheWrapper<TResult>>>();
        Exception? exception = null;
        var wasHit = true;
        
        // Cache miss - execute and store
        try
        {
            if(_memoryCache.TryGetValue(cacheKey, out var cachedResult) && cachedResult is TResult memoryCache)
            {
                return (memoryCache, null, true);
            }
            
            var wrapper = await cache.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    try
                    {
                        var result = await func();
                        wasHit = false;
                        return new AutoCacheWrapper<TResult>
                        {
                            Value = result
                        };
                    }
                    catch (Exception e)
                    {
                        exception = new AutoCacheExceptionWrapper(
                            e
                        );
                        throw;
                    }
                },
                () =>
                {
                    var optionsResult = optionsFactory?.Invoke();
                    var options = optionsResult ?? new DistributedCacheEntryOptions();
                    if (optionsResult == null && _options.DefaultAbsoluteExpirationRelativeToNow > 0)
                    {
                        options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(_options.DefaultAbsoluteExpirationRelativeToNow);
                    }

                    if (optionsResult == null && _options.DefaultSlidingExpiration > 0)
                    {
                        options.SlidingExpiration =
                            TimeSpan.FromMilliseconds(_options.DefaultSlidingExpiration);
                    }
                    
                    return options;
                },
                considerUow: considerUow
            );

            var result = wrapper == null ? default : wrapper.Value;
            
            _memoryCache.TryAdd(cacheKey, result);
            
            return (result, exception, wasHit);
        }
        catch (Exception e)
        {
            if (e is AutoCacheExceptionWrapper exceptionWrapper)
            {
                throw exceptionWrapper.OriginalException;
            }
            
            await cache.RemoveAsync(cacheKey);
            return (default, e, wasHit);
        }
    }

    private string GenerateCacheKey<TResult>(string className, string? additionalCacheKey, string methodName, object?[] parameters, AutoCacheScope scope)
    {
        var parametersJson = string.Join(AutoCacheConstants.ParameterSeparator, parameters.Select(p => 
            p == null ? AutoCacheConstants.NullParameterValue : JsonSerializer.Serialize(p)
        ));
        
        additionalCacheKey ??= string.Empty;
        
        var scopeKeyBuilder = new List<string>(2);
        if(scope.HasFlag(AutoCacheScope.CurrentUser))
        {
            var userId = _currentUser.Id?.ToString() ?? AutoCacheConstants.Scopes.Anonymous;
            scopeKeyBuilder.Add(userId);
        }
        
        if(scope.HasFlag(AutoCacheScope.AuthenticatedUser))
        {
            var authScope = _currentUser.IsAuthenticated 
                ? AutoCacheConstants.Scopes.Authenticated 
                : AutoCacheConstants.Scopes.Unauthenticated;
            scopeKeyBuilder.Add(authScope);
        }

        var scopeKey = scopeKeyBuilder.Count > 0 
            ? string.Join(AutoCacheConstants.KeySeparator, scopeKeyBuilder) 
            : string.Empty;

        var baseKey = $"{_options.KeyPrefix}{AutoCacheConstants.KeySeparator}{additionalCacheKey}{AutoCacheConstants.KeySeparator}{scopeKey}{AutoCacheConstants.KeySeparator}{className}{AutoCacheConstants.KeySeparator}{typeof(TResult).FullName}{AutoCacheConstants.KeySeparator}{methodName}{AutoCacheConstants.KeySeparator}{parametersJson}";
        
        // Apply compression if the key is too long
        if (!_options.EnableKeyCompression || baseKey.Length <= _options.MaxKeyLengthBeforeCompression)
        {
            return baseKey;
        }

        var hash = parametersJson.ToMd5();
        return $"{_options.KeyPrefix}{AutoCacheConstants.KeySeparator}{additionalCacheKey}{AutoCacheConstants.KeySeparator}{scopeKey}{AutoCacheConstants.KeySeparator}{className}{AutoCacheConstants.KeySeparator}{typeof(TResult).FullName}{AutoCacheConstants.KeySeparator}{methodName}{AutoCacheConstants.KeySeparator}{hash}";
    }
}
