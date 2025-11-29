using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;
using AutoCacheDemo.Books;

namespace AutoCacheDemo.Web;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class AutoCacheDemoWebMappers : MapperBase<BookDto, CreateUpdateBookDto>
{
    public override partial CreateUpdateBookDto Map(BookDto source);

    public override partial void Map(BookDto source, CreateUpdateBookDto destination);
}
