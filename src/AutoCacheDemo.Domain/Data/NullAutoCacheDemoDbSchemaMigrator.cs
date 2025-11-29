using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AutoCacheDemo.Data;

/* This is used if database provider does't define
 * IAutoCacheDemoDbSchemaMigrator implementation.
 */
public class NullAutoCacheDemoDbSchemaMigrator : IAutoCacheDemoDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
