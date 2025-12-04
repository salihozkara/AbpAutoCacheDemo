using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace AutoCache;

[DependsOn(typeof(AbpDddDomainModule), typeof(AbpCachingStackExchangeRedisModule))]
public class AutoCacheModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.OnRegistered(AutoCacheRegister.RegisterInterceptorIfNeeded);
    }
}