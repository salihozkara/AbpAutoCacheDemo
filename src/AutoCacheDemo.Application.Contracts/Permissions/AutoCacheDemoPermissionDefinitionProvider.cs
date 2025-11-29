using AutoCacheDemo.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace AutoCacheDemo.Permissions;

public class AutoCacheDemoPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(AutoCacheDemoPermissions.GroupName);

        var booksPermission = myGroup.AddPermission(AutoCacheDemoPermissions.Books.Default, L("Permission:Books"));
        booksPermission.AddChild(AutoCacheDemoPermissions.Books.Create, L("Permission:Books.Create"));
        booksPermission.AddChild(AutoCacheDemoPermissions.Books.Edit, L("Permission:Books.Edit"));
        booksPermission.AddChild(AutoCacheDemoPermissions.Books.Delete, L("Permission:Books.Delete"));
        //Define your own permissions here. Example:
        //myGroup.AddPermission(AutoCacheDemoPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AutoCacheDemoResource>(name);
    }
}
