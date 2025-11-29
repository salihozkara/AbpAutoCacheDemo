using AutoCacheDemo.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AutoCacheDemo.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AutoCacheDemoEntityFrameworkCoreModule),
    typeof(AutoCacheDemoApplicationContractsModule)
)]
public class AutoCacheDemoDbMigratorModule : AbpModule
{
}
