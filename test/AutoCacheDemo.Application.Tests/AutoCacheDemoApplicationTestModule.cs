using Volo.Abp.Modularity;

namespace AutoCacheDemo;

[DependsOn(
    typeof(AutoCacheDemoApplicationModule),
    typeof(AutoCacheDemoDomainTestModule)
)]
public class AutoCacheDemoApplicationTestModule : AbpModule
{

}
