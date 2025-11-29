using AutoCacheDemo.Books;
using Xunit;

namespace AutoCacheDemo.EntityFrameworkCore.Applications.Books;

[Collection(AutoCacheDemoTestConsts.CollectionDefinitionName)]
public class EfCoreBookAppService_Tests : BookAppService_Tests<AutoCacheDemoEntityFrameworkCoreTestModule>
{

}