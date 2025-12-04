using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.DynamicProxy;

namespace AutoCache;

/// <summary>
/// Registration helpers for AutoCache system
/// </summary>
public static class AutoCacheRegister
{
    /// <summary>
    /// Registers the AutoCache interceptor for classes that have methods decorated with AutoCacheAttribute
    /// </summary>
    public static void RegisterInterceptorIfNeeded(IOnServiceRegistredContext context)
    {
        if (!DynamicProxyIgnoreTypes.Contains(context.ImplementationType) && 
            context.ImplementationType.GetMethods().Any(m => m.IsDefined(typeof(CacheAttribute), true)))
        {
            context.Interceptors.TryAdd<AutoCacheInterceptor>();
        }
    }

    /// <summary>
    /// Adds the CacheManager for a specific entity type to handle cache invalidation
    /// </summary>
    /// <param name="services"></param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the CacheManager for a specific entity type to handle cache invalidation
        /// </summary>
        public void AddAutoCache<TEntity>() where TEntity : class, IEntity
        {
            services.TryAddTransient<AutoCacheInvalidationHandler<TEntity>>();
        }
        
        public void AddAutoCache<TEntity, TKey>() where TEntity : class, IEntity<TKey>
        {
            services.TryAddTransient<AutoCacheInvalidationHandler<TEntity, TKey>>();
        }

        /// <summary>
        /// Adds the CacheManager for a specific entity type with a user ID selector to handle cache invalidation
        /// </summary>
        /// <param name="userIdSelector"></param>
        /// <typeparam name="TEntity"></typeparam>
        public void AddAutoCacheWithUserIdSelector<TEntity>(Func<TEntity, Guid?> userIdSelector) where TEntity : class, IEntity
        {
            services.TryAddTransient<AutoCacheInvalidationHandler<TEntity>>();
            services.Configure<AutoCacheUserIdSelectorOptions>(options =>
            {
                options.AddUserIdSelector(userIdSelector);
            });
        }

        /// <summary>
        /// Adds the CacheManager for a specific entity type with a user ID selector to handle cache invalidation
        /// </summary>
        /// <param name="userIdListSelector"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        public void AddAutoCacheWithUserIdListSelector<TEntity, TKey>(Func<TEntity, List<Guid?>> userIdListSelector) where TEntity : class, IEntity<TKey>
        {
            services.TryAddTransient<AutoCacheInvalidationHandler<TEntity, TKey>>();
            services.Configure<AutoCacheUserIdSelectorOptions>(options =>
            {
                options.AddUserIdListSelector(userIdListSelector);
            });
        }
    }
}