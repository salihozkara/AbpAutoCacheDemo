using AutoCacheDemo.Samples;
using Xunit;

namespace AutoCacheDemo.EntityFrameworkCore.Applications;

[Collection(AutoCacheDemoTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<AutoCacheDemoEntityFrameworkCoreTestModule>
{

}
