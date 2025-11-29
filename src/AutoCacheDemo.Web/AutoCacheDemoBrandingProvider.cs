using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.Localization;
using AutoCacheDemo.Localization;

namespace AutoCacheDemo.Web;

[Dependency(ReplaceServices = true)]
public class AutoCacheDemoBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<AutoCacheDemoResource> _localizer;

    public AutoCacheDemoBrandingProvider(IStringLocalizer<AutoCacheDemoResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
