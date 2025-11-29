using Volo.Abp.Modularity;

namespace AutoCacheDemo;

/* Inherit from this class for your domain layer tests. */
public abstract class AutoCacheDemoDomainTestBase<TStartupModule> : AutoCacheDemoTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
