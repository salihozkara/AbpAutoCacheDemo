using System.Threading.Tasks;

namespace AutoCacheDemo.Data;

public interface IAutoCacheDemoDbSchemaMigrator
{
    Task MigrateAsync();
}
