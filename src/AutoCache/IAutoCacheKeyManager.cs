using System;
using System.Threading.Tasks;

namespace AutoCache;

public interface IAutoCacheKeyManager
{
    Task RemoveCacheAndCacheKeys(Type entityType, RemoveCacheKeyContext context);
    Task AddCacheKeysAsync(Type[] entityTypes, string key, AddCacheKeyContext context);
}