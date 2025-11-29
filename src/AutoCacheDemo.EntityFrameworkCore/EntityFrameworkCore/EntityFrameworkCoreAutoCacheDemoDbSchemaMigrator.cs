using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AutoCacheDemo.Data;
using Volo.Abp.DependencyInjection;

namespace AutoCacheDemo.EntityFrameworkCore;

public class EntityFrameworkCoreAutoCacheDemoDbSchemaMigrator
    : IAutoCacheDemoDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreAutoCacheDemoDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the AutoCacheDemoDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<AutoCacheDemoDbContext>()
            .Database
            .MigrateAsync();
    }
}
