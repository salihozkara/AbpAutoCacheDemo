using System;
using AutoCache;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Volo.Abp.Mapperly;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.TenantManagement;

namespace AutoCacheDemo;

[DependsOn(
    typeof(AutoCacheDemoDomainModule),
    typeof(AutoCacheDemoApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class AutoCacheDemoApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AutoCacheOptions>(options =>
        {
            options.AddKeyParametersSelector(o =>
            {
                if (o is not IEntityDto entityDto)
                {
                    return null;
                }

                var idProperty = entityDto.GetType().GetProperty("Id");
                if (idProperty != null)
                {
                    return [idProperty.GetValue(entityDto)];
                }

                return null;
            });
        });
    }
}
