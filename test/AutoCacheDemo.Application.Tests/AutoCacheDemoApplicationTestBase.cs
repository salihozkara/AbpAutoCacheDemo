using Volo.Abp.Modularity;

namespace AutoCacheDemo;

public abstract class AutoCacheDemoApplicationTestBase<TStartupModule> : AutoCacheDemoTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
