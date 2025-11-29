using Volo.Abp.Settings;

namespace AutoCacheDemo.Settings;

public class AutoCacheDemoSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(AutoCacheDemoSettings.MySetting1));
    }
}
