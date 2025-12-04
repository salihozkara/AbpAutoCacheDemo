using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using AutoCacheDemo.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using System.Linq.Dynamic.Core;
using AutoCache;

namespace AutoCacheDemo.Books;

[Authorize(AutoCacheDemoPermissions.Books.Default)]
public class BookAppService : ApplicationService, IBookAppService
{
    private readonly IRepository<Book, Guid> _repository;
    private readonly AutoCacheManager _autoCacheManager;

    public BookAppService(IRepository<Book, Guid> repository, AutoCacheManager autoCacheManager)
    {
        _repository = repository;
        _autoCacheManager = autoCacheManager;
    }

    [Cache(typeof(Book), Scope = AutoCacheScope.Global)]
    public virtual async Task<BookDto> GetAsync(Guid id)
    {
        var book = await _autoCacheManager.GetOrAddAsync(this, async () => await _repository.GetAsync(id), [id], invalidateOnEntities:
            [typeof(Book)], scope: AutoCacheScope.Entity);
        return ObjectMapper.Map<Book, BookDto>(book!);
    }

    [Cache(typeof(Book))]
    public virtual async Task<PagedResultDto<BookDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var queryable = await _repository.GetQueryableAsync();
        var query = queryable
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Name" : input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var books = await AsyncExecuter.ToListAsync(query);
        var totalCount = await AsyncExecuter.CountAsync(queryable);

        return new PagedResultDto<BookDto>(
            totalCount,
            ObjectMapper.Map<List<Book>, List<BookDto>>(books)
        );
    }

    [Authorize(AutoCacheDemoPermissions.Books.Create)]
    public async Task<BookDto> CreateAsync(CreateUpdateBookDto input)
    {
        var book = ObjectMapper.Map<CreateUpdateBookDto, Book>(input);
        await _repository.InsertAsync(book);
        return ObjectMapper.Map<Book, BookDto>(book);
    }

    [Authorize(AutoCacheDemoPermissions.Books.Edit)]
    public async Task<BookDto> UpdateAsync(Guid id, CreateUpdateBookDto input)
    {
        var book = await _repository.GetAsync(id);
        ObjectMapper.Map(input, book);
        await _repository.UpdateAsync(book);
        return ObjectMapper.Map<Book, BookDto>(book);
    }

    [Authorize(AutoCacheDemoPermissions.Books.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }
}
