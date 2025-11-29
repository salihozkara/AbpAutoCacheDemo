using AutoCacheDemo.Localization;
using Volo.Abp.Application.Services;

namespace AutoCacheDemo;

/* Inherit your application services from this class.
 */
public abstract class AutoCacheDemoAppService : ApplicationService
{
    protected AutoCacheDemoAppService()
    {
        LocalizationResource = typeof(AutoCacheDemoResource);
    }
}
