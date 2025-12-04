using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DynamicProxy;

namespace AutoCache;

/// <summary>
/// Interceptor that automatically caches method results based on AutoCacheAttribute
/// </summary>
public class AutoCacheInterceptor : AbpInterceptor, ITransientDependency
{
    private readonly ILogger<AutoCacheInterceptor> _logger;
    private readonly AutoCacheOptions _options;
    private static readonly MethodInfo GetOrAddCacheAsyncMethod;
    private readonly AutoCacheManager _autoCacheManager;
    private static readonly ConcurrentDictionary<Type, MethodInfo> MethodCache = new();

    static AutoCacheInterceptor()
    {
        GetOrAddCacheAsyncMethod = typeof(AutoCacheInterceptor).GetMethod(
            nameof(GetOrAddCacheAsync),
            BindingFlags.NonPublic | BindingFlags.Instance
        )!;
    }

    public AutoCacheInterceptor(
        ILogger<AutoCacheInterceptor> logger,
        IOptions<AutoCacheOptions> options, 
        AutoCacheManager autoCacheManager)
    {
        _logger = logger;
        _autoCacheManager = autoCacheManager;
        _options = options.Value;
    }

    public override async Task InterceptAsync(IAbpMethodInvocation invocation)
    {
        if(!_options.Enabled || invocation.Method.GetCustomAttributes(typeof(CacheAttribute), true).FirstOrDefault() is not CacheAttribute attribute)
        {
            await invocation.ProceedAsync();
            return;
        }
        
        var proceeded = false;

        try
        {
            var genericMethod = MethodCache.GetOrAdd(invocation.Method.ReturnType, t =>
            {
                var isGenericTask = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>);
                var resultType = isGenericTask ? t.GetGenericArguments()[0] : t;
                return GetOrAddCacheAsyncMethod.MakeGenericMethod(resultType);
            });
            
            (var result, proceeded) = await (Task<(object, bool)>)genericMethod.Invoke(this, [invocation, attribute])!;
            invocation.ReturnValue = result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while caching method {MethodName}", invocation.Method.Name);
            
            if(e is AutoCacheExceptionWrapper exceptionWrapper)
            {
                if (_options.ThrowOnError)
                {
                    throw exceptionWrapper.OriginalException;
                }
                
                _logger.LogWarning(
                    "Cache operation failed, falling back to method execution for {MethodName}",
                    invocation.Method.Name
                );
            }

            if (!proceeded && invocation.ReturnValue == null)
            {
                await invocation.ProceedAsync();
            }
        }
    }

    private async Task<(object?, bool)> GetOrAddCacheAsync<TResult>(IAbpMethodInvocation invocation, CacheAttribute attribute)
    {
        var proceeded = false;
        var result = await _autoCacheManager.GetOrAddAsync(invocation.TargetObject, Factory, invocation.Arguments, () => new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = GetExpiration(attribute.AbsoluteExpirationRelativeToNow, _options.DefaultAbsoluteExpirationRelativeToNow),
            SlidingExpiration = GetExpiration(attribute.SlidingExpiration, _options.DefaultSlidingExpiration)
        }, attribute.InvalidateOnEntities, attribute.Scope, attribute.ConsiderUow, attribute.AdditionalCacheKey, invocation.Method.Name);
        
        return (result, proceeded);

        async Task<TResult> Factory()
        {
            await invocation.ProceedAsync();
            proceeded = true;
            return (TResult)invocation.ReturnValue;
        }
    }
    
    private static TimeSpan? GetExpiration(long milliseconds, long defaultValue)
    {
        return milliseconds switch
        {
            0 => defaultValue > 0 ? TimeSpan.FromMilliseconds(defaultValue) : null,
            < 0 => null,
            _ => TimeSpan.FromMilliseconds(milliseconds)
        };
    }
}
