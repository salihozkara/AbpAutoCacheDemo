using Xunit;

namespace AutoCacheDemo.EntityFrameworkCore;

[CollectionDefinition(AutoCacheDemoTestConsts.CollectionDefinitionName)]
public class AutoCacheDemoEntityFrameworkCoreCollection : ICollectionFixture<AutoCacheDemoEntityFrameworkCoreFixture>
{

}
