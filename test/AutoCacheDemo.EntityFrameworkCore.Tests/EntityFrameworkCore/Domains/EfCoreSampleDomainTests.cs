using AutoCacheDemo.Samples;
using Xunit;

namespace AutoCacheDemo.EntityFrameworkCore.Domains;

[Collection(AutoCacheDemoTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<AutoCacheDemoEntityFrameworkCoreTestModule>
{

}
