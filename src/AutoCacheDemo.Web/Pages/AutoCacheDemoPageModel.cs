using AutoCacheDemo.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace AutoCacheDemo.Web.Pages;

public abstract class AutoCacheDemoPageModel : AbpPageModel
{
    protected AutoCacheDemoPageModel()
    {
        LocalizationResourceType = typeof(AutoCacheDemoResource);
    }
}
