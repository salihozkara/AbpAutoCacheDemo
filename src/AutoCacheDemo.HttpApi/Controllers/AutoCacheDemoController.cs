using AutoCacheDemo.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace AutoCacheDemo.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class AutoCacheDemoController : AbpControllerBase
{
    protected AutoCacheDemoController()
    {
        LocalizationResource = typeof(AutoCacheDemoResource);
    }
}
