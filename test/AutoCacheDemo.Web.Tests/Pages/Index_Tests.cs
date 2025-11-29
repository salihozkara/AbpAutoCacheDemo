using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AutoCacheDemo.Pages;

[Collection(AutoCacheDemoTestConsts.CollectionDefinitionName)]
public class Index_Tests : AutoCacheDemoWebTestBase
{
    [Fact]
    public async Task Welcome_Page()
    {
        var response = await GetResponseAsStringAsync("/");
        response.ShouldNotBeNull();
    }
}
