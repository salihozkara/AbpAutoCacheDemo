using Volo.Abp.Modularity;

namespace AutoCacheDemo;

[DependsOn(
    typeof(AutoCacheDemoDomainModule),
    typeof(AutoCacheDemoTestBaseModule)
)]
public class AutoCacheDemoDomainTestModule : AbpModule
{

}
