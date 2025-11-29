using Microsoft.AspNetCore.Builder;
using AutoCacheDemo;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("AutoCacheDemo.Web.csproj"); 
await builder.RunAbpModuleAsync<AutoCacheDemoWebTestModule>(applicationName: "AutoCacheDemo.Web");

public partial class Program
{
}
