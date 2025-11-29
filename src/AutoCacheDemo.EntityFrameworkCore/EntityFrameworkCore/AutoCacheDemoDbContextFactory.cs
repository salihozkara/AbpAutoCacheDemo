using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AutoCacheDemo.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class AutoCacheDemoDbContextFactory : IDesignTimeDbContextFactory<AutoCacheDemoDbContext>
{
    public AutoCacheDemoDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        
        AutoCacheDemoEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<AutoCacheDemoDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));
        
        return new AutoCacheDemoDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../AutoCacheDemo.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
