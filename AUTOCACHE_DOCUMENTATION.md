# AutoCache - Attribute-Based Automatic Caching Library for ABP Framework

## 📋 Table of Contents

1. [Overview](#overview)
2. [Key Features](#key-features)
3. [Architecture](#architecture)
4. [Installation & Setup](#installation--setup)
5. [Usage Guide](#usage-guide)
6. [Configuration](#configuration)
7. [Advanced Scenarios](#advanced-scenarios)
8. [How It Works](#how-it-works)
9. [Performance & Metrics](#performance--metrics)
10. [Best Practices](#best-practices)
11. [Troubleshooting](#troubleshooting)
12. [API Reference](#api-reference)

---

## 📋 Overview

**AutoCache** is an attribute-based automatic caching library specifically designed for ABP Framework applications. It provides method-level caching with automatic cache invalidation based on entity changes, eliminating the need for manual cache management.

### What Problem Does It Solve?

Manual cache management in applications often leads to:
- **Repetitive code**: Writing cache get/set logic for every method
- **Cache invalidation complexity**: Tracking which caches to clear when entities change
- **Stale data risks**: Forgetting to invalidate related caches
- **Maintenance overhead**: Updating cache logic when requirements change

AutoCache solves these problems by providing:
- Declarative caching with a single `[Cache]` attribute
- Automatic cache invalidation when entities change
- Scope-based caching (Global, User-specific, Entity-specific)
- Built-in metrics and monitoring

---

## 🎯 Key Features

### 1. **Attribute-Based Caching**
Mark methods with `[Cache]` attribute for automatic caching - no manual cache management code needed.

```csharp
[Cache(typeof(Book), Scope = AutoCacheScope.Global)]
public virtual async Task<PagedResultDto<BookDto>> GetListAsync(PagedAndSortedResultRequestDto input)
{
    // Method result is automatically cached
    return result;
}
```

### 2. **Automatic Cache Invalidation**
When entities are created, updated, or deleted, all related caches are automatically cleared.

```csharp
// When a Book is updated, all caches with [Cache(typeof(Book))] are cleared
await _repository.UpdateAsync(book);
```

### 3. **Flexible Caching Scopes**

| Scope | Description | Use Case |
|-------|-------------|----------|
| `Global` | Shared across all users | Public data, lookup tables |
| `CurrentUser` | Per user cache | User-specific data |
| `AuthenticatedUser` | Separate for authenticated/anonymous | Public pages with auth variations |
| `Entity` | Per entity primary key | Single entity queries by ID |

### 4. **Redis-Based Distributed Cache**
Built on top of StackExchange.Redis and ABP's caching infrastructure for distributed scenarios.

### 5. **Unit of Work Integration**
Cache invalidation happens after transaction commit, ensuring data consistency.

```csharp
[Cache(typeof(Book), ConsiderUow = true)]
public virtual async Task<BookDto> GetAsync(Guid id)
{
    // If transaction rolls back, cache is NOT cleared
}
```

### 6. **Built-in Metrics & Monitoring**
Track cache hit/miss rates, errors, and performance metrics.

```csharp
var stats = _metrics.GetStatistics();
Console.WriteLine($"Hit Rate: {stats.HitRate}%");
```

### 7. **Key Compression**
Automatically compresses long cache keys using MD5 hashing to stay within Redis key length limits.

### 8. **Hybrid Memory + Distributed Cache**
Uses in-memory caching for the current request scope + Redis for distributed caching.

---

## 🏗️ Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      Application Layer                       │
│  ┌──────────────────────────────────────────────────────┐  │
│  │   [Cache] Attribute on Methods                       │  │
│  └──────────────────┬───────────────────────────────────┘  │
│                     │                                        │
│                     ▼                                        │
│  ┌──────────────────────────────────────────────────────┐  │
│  │         AutoCacheInterceptor (ABP Interceptor)       │  │
│  └──────────────────┬───────────────────────────────────┘  │
└────────────────────┬┼──────────────────────────────────────┘
                     ││
                     ▼▼
┌─────────────────────────────────────────────────────────────┐
│                    AutoCache Core Layer                      │
│  ┌──────────────────────────────────────────────────────┐  │
│  │            AutoCacheManager                           │  │
│  │  • Generate cache keys                                │  │
│  │  • Get/Add cache entries                              │  │
│  │  • Coordinate with metrics                            │  │
│  └──────────────┬───────────────────┬───────────────────┘  │
│                 │                   │                        │
│                 ▼                   ▼                        │
│  ┌──────────────────────┐  ┌──────────────────────────┐   │
│  │ RedisAutoCacheKey    │  │  AutoCacheMetrics        │   │
│  │ Manager              │  │  • Hit/Miss tracking     │   │
│  │ • Store cache keys   │  │  • Error logging         │   │
│  │ • Invalidate caches  │  └──────────────────────────┘   │
│  └──────────┬───────────┘                                   │
└─────────────┼───────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────┐
│                Entity Change Events                          │
│  ┌──────────────────────────────────────────────────────┐  │
│  │   AutoCacheInvalidationHandler<TEntity>              │  │
│  │   • Listens to EntityChangedEventData                │  │
│  │   • Triggers cache invalidation on entity changes    │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
              │
              ▼
       ┌──────────────┐
       │    Redis     │
       │   Cache      │
       └──────────────┘
```

### Core Components

#### 1. **AutoCacheModule**
ABP module that registers all services and interceptors.

```csharp
[DependsOn(typeof(AbpDddDomainModule), typeof(AbpCachingStackExchangeRedisModule))]
public class AutoCacheModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.OnRegistered(AutoCacheRegister.RegisterInterceptorIfNeeded);
    }
}
```

#### 2. **CacheAttribute**
Marks methods for automatic caching.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class CacheAttribute : Attribute
{
    public Type[] InvalidateOnEntities { get; set; }
    public AutoCacheScope Scope { get; set; } = AutoCacheScope.Global;
    public long AbsoluteExpirationRelativeToNow { get; set; }
    public long SlidingExpiration { get; set; }
    public bool ConsiderUow { get; set; }
    public string AdditionalCacheKey { get; set; }
}
```

#### 3. **AutoCacheInterceptor**
ABP interceptor that intercepts method calls and applies caching logic.

**Location**: `AutoCacheInterceptor.cs:43-87`

#### 4. **AutoCacheManager**
Core cache management logic - generates keys, gets/adds cache entries.

**Location**: `AutoCacheManager.cs:23-237`

#### 5. **RedisAutoCacheKeyManager**
Manages cache keys in Redis using Sets for efficient invalidation.

**Location**: `RedisAutoCacheKeyManager.cs:17-137`

#### 6. **AutoCacheInvalidationHandler<TEntity>**
Event handler that listens to entity changes and triggers cache invalidation.

**Location**: `AutoCacheInvalidationHandler.cs:16-154`

---

## 🔧 Installation & Setup

### Step 1: Add AutoCache Module

Add the AutoCache library to your project and configure the module dependency:

```csharp
// YourDomainModule.cs
[DependsOn(
    // ... other dependencies
    typeof(AutoCacheModule)
)]
public class YourDomainModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Register entities for cache invalidation
        context.Services.AddAutoCache<Book, Guid>();
        context.Services.AddAutoCache<Author, Guid>();
        context.Services.AddAutoCache<Category, int>();
    }
}
```

### Step 2: Configure Redis

Ensure Redis is configured in your ABP application:

```json
// appsettings.json
{
  "Redis": {
    "Configuration": "127.0.0.1:6379"
  },
  "AbpDistributedCache": {
    "KeyPrefix": "MyApp"
  }
}
```

### Step 3: Configure AutoCache Options (Optional)

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    Configure<AutoCacheOptions>(options =>
    {
        options.Enabled = true;
        options.DefaultAbsoluteExpirationRelativeToNow = 300000; // 5 minutes
        options.DefaultSlidingExpiration = 0; // Disabled
        options.EnableMetrics = true;
        options.EnableLogging = false;
        options.EnableKeyCompression = true;
        options.MaxKeyLengthBeforeCompression = 250;
        options.KeyPrefix = "AutoCache";
        options.ThrowOnError = false; // Fallback to method execution on error
    });
}
```

---

## 📚 Usage Guide

### Basic Usage: Attribute-Based Caching

The simplest way to use AutoCache is with the `[Cache]` attribute:

```csharp
public class BookAppService : ApplicationService, IBookAppService
{
    private readonly IRepository<Book, Guid> _repository;

    public BookAppService(IRepository<Book, Guid> repository)
    {
        _repository = repository;
    }

    // Cache this method's result
    // When any Book entity changes, this cache will be cleared
    [Cache(typeof(Book))]
    public virtual async Task<PagedResultDto<BookDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var queryable = await _repository.GetQueryableAsync();
        var query = queryable
            .OrderBy(input.Sorting ?? "Name")
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var books = await AsyncExecuter.ToListAsync(query);
        var totalCount = await AsyncExecuter.CountAsync(queryable);

        return new PagedResultDto<BookDto>(
            totalCount,
            ObjectMapper.Map<List<Book>, List<BookDto>>(books)
        );
    }
}
```

**Important**: The method **must be virtual** for interception to work!

### Manual Usage: AutoCacheManager

For more control, inject and use `AutoCacheManager`:

```csharp
public class BookAppService : ApplicationService
{
    private readonly IRepository<Book, Guid> _repository;
    private readonly AutoCacheManager _autoCacheManager;

    public BookAppService(
        IRepository<Book, Guid> repository,
        AutoCacheManager autoCacheManager)
    {
        _repository = repository;
        _autoCacheManager = autoCacheManager;
    }

    public async Task<BookDto> GetAsync(Guid id)
    {
        var book = await _autoCacheManager.GetOrAddAsync(
            caller: this,
            func: async () => await _repository.GetAsync(id),
            parameters: new object[] { id },
            invalidateOnEntities: new[] { typeof(Book) },
            scope: AutoCacheScope.Entity
        );

        return ObjectMapper.Map<Book, BookDto>(book!);
    }
}
```

### Multiple Entity Dependencies

When a cache depends on multiple entity types:

```csharp
[Cache(typeof(Book), typeof(Author), typeof(Category))]
public virtual async Task<List<BookDetailDto>> GetBooksWithDetailsAsync()
{
    // If Book, Author, or Category changes, this cache is cleared
    var books = await _repository.GetListAsync(includeDetails: true);
    return ObjectMapper.Map<List<Book>, List<BookDetailDto>>(books);
}
```

### Custom Cache Expiration

Override default expiration times:

```csharp
[Cache(
    typeof(Book),
    AbsoluteExpirationRelativeToNow = 60000,  // 1 minute
    SlidingExpiration = 30000                  // 30 seconds
)]
public virtual async Task<BookDto> GetAsync(Guid id)
{
    // Cache expires after 1 minute or 30 seconds of inactivity
}
```

---

## ⚙️ Configuration

### AutoCacheOptions Reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Master switch for the entire caching system |
| `DefaultAbsoluteExpirationRelativeToNow` | `long` | `300000` | Default cache duration in milliseconds (5 minutes) |
| `DefaultSlidingExpiration` | `long` | `0` | Default sliding expiration in milliseconds (disabled) |
| `EnableLogging` | `bool` | `false` | Log cache operations (HIT/MISS) |
| `EnableMetrics` | `bool` | `false` | Enable metrics collection |
| `ThrowOnError` | `bool` | `false` | Throw exceptions on cache errors (vs. fallback to method execution) |
| `KeyPrefix` | `string` | `"AutoCache"` | Global prefix for all cache keys |
| `EnableKeyCompression` | `bool` | `true` | Compress long keys using MD5 |
| `MaxKeyLengthBeforeCompression` | `int` | `250` | Key length threshold for compression |

### CacheAttribute Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `InvalidateOnEntities` | `Type[]` | Required | Entity types that trigger cache invalidation |
| `Scope` | `AutoCacheScope` | `Global` | Cache scope (Global/CurrentUser/AuthenticatedUser/Entity) |
| `AbsoluteExpirationRelativeToNow` | `long` | `0` | Override default expiration (ms), -1 to disable |
| `SlidingExpiration` | `long` | `0` | Override default sliding expiration (ms), -1 to disable |
| `ConsiderUow` | `bool` | `false` | Wait for Unit of Work completion before invalidation |
| `AdditionalCacheKey` | `string` | `null` | Extra key component for differentiation |

---

## 🚀 Advanced Scenarios

### User-Specific Caching

Cache data separately for each user:

```csharp
[Cache(typeof(Book), Scope = AutoCacheScope.CurrentUser)]
public virtual async Task<List<BookDto>> GetMyFavoriteBooksAsync()
{
    // Each user gets their own cache entry
    var userId = CurrentUser.GetId();
    var books = await _repository.GetListAsync(x => x.CreatorId == userId);
    return ObjectMapper.Map<List<Book>, List<BookDto>>(books);
}
```

### User ID Selectors for Smart Invalidation

When a Book is updated, clear only the cache of the user who created it:

```csharp
// In Domain Module
context.Services.AddAutoCacheWithUserIdSelector<Book>(
    book => book.CreatorId
);

// Now when a Book changes, only that user's CurrentUser-scoped caches are cleared
```

### User ID List Selectors

For entities with multiple related users (e.g., shared documents):

```csharp
public class Document : Entity<Guid>
{
    public List<Guid> SharedWithUserIds { get; set; }
}

// In Domain Module
context.Services.AddAutoCacheWithUserIdListSelector<Document, Guid>(
    document => document.SharedWithUserIds.Select(x => (Guid?)x).ToList()
);
```

### Entity-Scoped Caching

Cache individual entity queries by their primary key:

```csharp
[Cache(typeof(Book), Scope = AutoCacheScope.Entity)]
public virtual async Task<BookDto> GetAsync(Guid id)
{
    // Each book ID has its own cache entry
    // When Book with ID=X is updated, only that specific cache is cleared
    var book = await _autoCacheManager.GetOrAddAsync(
        this,
        async () => await _repository.GetAsync(id),
        parameters: new object[] { id },
        invalidateOnEntities: new[] { typeof(Book) },
        scope: AutoCacheScope.Entity
    );

    return ObjectMapper.Map<Book, BookDto>(book);
}
```

### Authenticated vs. Anonymous Caching

Cache public pages differently for authenticated and anonymous users:

```csharp
[Cache(typeof(Article), Scope = AutoCacheScope.AuthenticatedUser)]
public virtual async Task<List<ArticleDto>> GetPublicArticlesAsync()
{
    // Authenticated users might see additional data (e.g., favorites)
    // Anonymous users see basic data
    // Two separate cache entries are maintained
}
```

### Custom Key Parameters

Differentiate cache entries with additional keys:

```csharp
[Cache(typeof(Report), AdditionalCacheKey = "Monthly")]
public virtual async Task<ReportDto> GetMonthlyReportAsync(int year, int month)
{
    // Cache key includes "Monthly" to differentiate from other report types
}

[Cache(typeof(Report), AdditionalCacheKey = "Yearly")]
public virtual async Task<ReportDto> GetYearlyReportAsync(int year)
{
    // Different cache than monthly reports
}
```

### Unit of Work Integration

Ensure cache invalidation happens only after successful transaction commit:

```csharp
[Cache(typeof(Order), ConsiderUow = true)]
public virtual async Task<OrderDto> GetOrderAsync(Guid orderId)
{
    // If order update transaction rolls back, cache is NOT cleared
}

[UnitOfWork]
public virtual async Task ProcessOrderAsync(Guid orderId)
{
    var order = await _orderRepository.GetAsync(orderId);
    order.Status = OrderStatus.Processing;

    // If exception occurs here, transaction rolls back
    // and GetOrderAsync cache is NOT invalidated
    await _paymentService.ChargeAsync(order);

    await _orderRepository.UpdateAsync(order);
    // Cache is cleared AFTER successful commit
}
```

---

## 🔍 How It Works

### Cache Key Generation

Cache keys are generated based on multiple factors:

**Format:**
```
{KeyPrefix}:{AdditionalKey}:{ScopeKey}:{ClassName}:{ReturnType}:{MethodName}:{Parameters}
```

**Example:**
```
AutoCache:::BookAppService:PagedResultDto:GetListAsync:{"SkipCount":0,"MaxResultCount":10}
```

**With CurrentUser scope:**
```
AutoCache::3fa85f64-5717-4562-b3fc-2c963f66afa6:BookAppService:List<BookDto>:GetMyBooksAsync:
```

**Key Compression:**
If the key exceeds `MaxKeyLengthBeforeCompression` (default 250), parameters are MD5 hashed:

```
AutoCache:::BookAppService:PagedResultDto:GetListAsync:a7f3e2b9c1d4...
```

### Cache Invalidation Flow

```
1. Entity Change (Insert/Update/Delete)
   ↓
2. EntityChangedEventData<TEntity> published
   ↓
3. AutoCacheInvalidationHandler<TEntity> receives event
   ↓
4. If ConsiderUow = true → Wait for UoW.OnCompleted()
   ↓
5. RedisAutoCacheKeyManager.RemoveCacheAndCacheKeys()
   ↓
6. Retrieve cache keys from Redis Set: "AutoCacheKeys:{EntityType}"
   ↓
7. Remove all cached values using retrieved keys
   ↓
8. Remove the Redis Set itself
```

### Redis Storage Structure

**Cache Values:**
```redis
Key: AutoCache::BookAppService:PagedResultDto:GetListAsync:{params}
Value: { "Value": { /* cached data */ }, "CachedAt": "2024-01-15T10:30:00Z" }
```

**Cache Key Sets (for invalidation):**
```redis
Key: AutoCacheKeys:Book
Type: Set
Members: [
  "AutoCache::BookAppService:PagedResultDto:GetListAsync:{params1}",
  "AutoCache::BookAppService:BookDto:GetAsync:{params2}",
  ...
]
```

**User-Specific Key Sets:**
```redis
Key: AutoCacheKeys:Book:3fa85f64-5717-4562-b3fc-2c963f66afa6
Type: Set
Members: [ /* caches for this user */ ]
```

**Entity-Specific Key Sets:**
```redis
Key: AutoCacheKeys:Book:PK:3fa85f64-5717-4562-b3fc-2c963f66afa6
Type: Set
Members: [ /* caches for this specific book */ ]
```

### Interception Process

1. **Method Call**: Application calls a method marked with `[Cache]`
2. **Interceptor**: `AutoCacheInterceptor` intercepts the call
3. **Key Generation**: Generate cache key based on method signature and parameters
4. **Cache Check**: Check Redis for cached value
   - **HIT**: Return cached value, record metric
   - **MISS**: Proceed with original method execution
5. **Method Execution**: Original method runs
6. **Cache Store**: Store result in Redis with configured expiration
7. **Key Registration**: Add cache key to entity's Redis Set for future invalidation
8. **Return**: Return result to caller

---

## 📊 Performance & Metrics

### Enabling Metrics

```csharp
Configure<AutoCacheOptions>(options =>
{
    options.EnableMetrics = true;
    options.EnableLogging = true;
});
```

### Accessing Metrics

```csharp
public class MetricsService : ITransientDependency
{
    private readonly IAutoCacheMetrics _metrics;

    public MetricsService(IAutoCacheMetrics metrics)
    {
        _metrics = metrics;
    }

    public void LogStatistics()
    {
        var stats = _metrics.GetStatistics();

        Console.WriteLine($"Total Operations: {stats.TotalOperations}");
        Console.WriteLine($"Cache Hits: {stats.TotalHits}");
        Console.WriteLine($"Cache Misses: {stats.TotalMisses}");
        Console.WriteLine($"Hit Rate: {stats.HitRate:F2}%");
        Console.WriteLine($"Errors: {stats.TotalErrors}");
    }

    public void ResetMetrics()
    {
        _metrics.Reset();
    }
}
```

### AutoCacheStatistics Properties

| Property | Type | Description |
|----------|------|-------------|
| `TotalHits` | `long` | Number of cache hits |
| `TotalMisses` | `long` | Number of cache misses |
| `TotalErrors` | `long` | Number of cache errors |
| `HitRate` | `double` | Hit percentage (0-100) |
| `TotalOperations` | `long` | TotalHits + TotalMisses |

### Performance Considerations

**Memory Cache Layer**: AutoCacheManager uses a `ConcurrentDictionary` for request-scoped memory caching:
```csharp
private readonly ConcurrentDictionary<string, object?> _memoryCache = new();
```

This provides:
- **Ultra-fast reads** within the same request
- **Reduced Redis calls** for repeated operations
- **Automatic cleanup** when request scope ends

**Key Compression**: Reduces Redis memory usage and network overhead for large parameter sets.

**Async Operations**: All cache operations are async, preventing thread blocking.

---

## ✅ Best Practices

### 1. Always Use Virtual Methods

```csharp
// ✅ CORRECT
[Cache(typeof(Book))]
public virtual async Task<BookDto> GetAsync(Guid id)

// ❌ WRONG - Interception won't work
[Cache(typeof(Book))]
public async Task<BookDto> GetAsync(Guid id)
```

### 2. Choose the Right Scope

| Scenario | Recommended Scope |
|----------|-------------------|
| Public data (same for all users) | `Global` |
| User-specific data | `CurrentUser` |
| Single entity queries | `Entity` |
| Public pages with auth variations | `AuthenticatedUser` |

### 3. Register Entities Properly

```csharp
// In DomainModule
context.Services.AddAutoCache<Book, Guid>();
context.Services.AddAutoCache<Author, Guid>();
```

Without registration, cache invalidation won't work!

### 4. Use ConsiderUow for Transactional Operations

```csharp
[Cache(typeof(Order), ConsiderUow = true)]
public virtual async Task<OrderDto> GetOrderAsync(Guid orderId)
```

Prevents cache invalidation on transaction rollback.

### 5. Set Appropriate Expiration Times

```csharp
// Frequently changing data - short expiration
[Cache(typeof(StockPrice), AbsoluteExpirationRelativeToNow = 10000)] // 10 seconds

// Rarely changing data - long expiration
[Cache(typeof(Category), AbsoluteExpirationRelativeToNow = 3600000)] // 1 hour
```

### 6. Use AdditionalCacheKey for Variants

```csharp
[Cache(typeof(Report), AdditionalCacheKey = "Summary")]
public virtual async Task<ReportDto> GetSummaryReportAsync()

[Cache(typeof(Report), AdditionalCacheKey = "Detailed")]
public virtual async Task<ReportDto> GetDetailedReportAsync()
```

### 7. Monitor Metrics in Production

```csharp
// Create a background job to monitor cache effectiveness
public class CacheMonitoringJob : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IAutoCacheMetrics _metrics;

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var stats = _metrics.GetStatistics();

        if (stats.HitRate < 50) // Less than 50% hit rate
        {
            // Alert: Cache is not effective
            await _alertService.SendAlertAsync("Low cache hit rate");
        }
    }
}
```

### 8. Handle Serialization Carefully

Ensure your DTOs are serializable:

```csharp
// ✅ CORRECT - Simple, serializable DTO
public class BookDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime PublishDate { get; set; }
}

// ❌ PROBLEMATIC - Circular references, complex types
public class BookDto
{
    public Book Entity { get; set; } // Contains navigation properties
    public IQueryable<Comment> Comments { get; set; } // Not serializable
}
```

---

## 🔧 Troubleshooting

### Cache Not Working

**Symptom**: Method results are not cached.

**Checklist**:
1. ✅ Is the method `virtual`?
2. ✅ Is `AutoCacheOptions.Enabled = true`?
3. ✅ Is Redis connection configured correctly?
4. ✅ Is the service registered with DI (not manually instantiated)?

**Debug**:
```csharp
Configure<AutoCacheOptions>(options =>
{
    options.EnableLogging = true; // Check logs for cache operations
});
```

### Cache Not Invalidating

**Symptom**: Stale data is returned after entity updates.

**Checklist**:
1. ✅ Is the entity registered with `AddAutoCache<TEntity>()`?
2. ✅ Is the entity type specified in `[Cache(typeof(Book))]`?
3. ✅ Is `ConsiderUow = true` preventing immediate invalidation?

**Debug**:
```csharp
// Check if invalidation handler is registered
var handler = _serviceProvider.GetService<AutoCacheInvalidationHandler<Book>>();
if (handler == null)
{
    // Handler not registered - add it in DomainModule
}
```

### Serialization Errors

**Symptom**: Cache operations fail with serialization exceptions.

**Solution**: Ensure cached types are JSON-serializable:
```csharp
// Use DTOs instead of entities
[Cache(typeof(Book))]
public virtual async Task<BookDto> GetAsync(Guid id) // ✅ DTO
{
    var book = await _repository.GetAsync(id);
    return ObjectMapper.Map<Book, BookDto>(book);
}

// Don't cache entities directly
[Cache(typeof(Book))]
public virtual async Task<Book> GetAsync(Guid id) // ❌ Entity
{
    return await _repository.GetAsync(id); // May have navigation properties
}
```

### Redis Connection Issues

**Symptom**: `RedisConnectionException` or cache operations failing.

**Solutions**:
1. Verify Redis is running: `redis-cli ping`
2. Check connection string in `appsettings.json`
3. Set `ThrowOnError = false` for graceful degradation:

```csharp
Configure<AutoCacheOptions>(options =>
{
    options.ThrowOnError = false; // Fallback to method execution
});
```

### Key Conflicts

**Symptom**: Different methods share the same cache entry.

**Solution**: Use `AdditionalCacheKey` to differentiate:

```csharp
[Cache(typeof(Book), AdditionalCacheKey = "List")]
public virtual async Task<List<BookDto>> GetListAsync()

[Cache(typeof(Book), AdditionalCacheKey = "Search")]
public virtual async Task<List<BookDto>> SearchAsync(string query)
```

---

## 📖 API Reference

### AutoCacheScope Enum

```csharp
[Flags]
public enum AutoCacheScope
{
    Global,           // Shared across all users
    CurrentUser,      // Per user (based on user ID)
    AuthenticatedUser,// Authenticated vs. unauthenticated
    Entity            // Per entity primary key
}
```

### AutoCacheManager Methods

```csharp
Task<TResult> GetOrAddAsync<TResult>(
    object? caller,
    Func<Task<TResult>> func,
    object?[]? parameters = null,
    Func<DistributedCacheEntryOptions>? optionsFactory = null,
    Type[]? invalidateOnEntities = null,
    AutoCacheScope scope = AutoCacheScope.Global,
    bool considerUow = false,
    string? additionalCacheKey = null,
    [CallerMemberName] string methodName = ""
)
```

### IAutoCacheMetrics Interface

```csharp
public interface IAutoCacheMetrics
{
    void RecordHit(string cacheKey);
    void RecordMiss(string cacheKey);
    void RecordError(string cacheKey, Exception exception);
    AutoCacheStatistics GetStatistics();
    void Reset();
}
```

### AutoCacheRegister Extensions

```csharp
// Register entity for cache invalidation
void AddAutoCache<TEntity>() where TEntity : class, IEntity

void AddAutoCache<TEntity, TKey>() where TEntity : class, IEntity<TKey>

// Register with user ID selector
void AddAutoCacheWithUserIdSelector<TEntity>(Func<TEntity, Guid?> userIdSelector)

void AddAutoCacheWithUserIdListSelector<TEntity, TKey>(Func<TEntity, List<Guid?>> userIdListSelector)
```

---

## 📦 Dependencies

- **Volo.Abp.Core** (10.0.0)
- **Volo.Abp.Ddd.Domain** (10.0.0)
- **Volo.Abp.Caching.StackExchangeRedis** (10.0.0)

---

## 🎓 Example: Complete Implementation

```csharp
// 1. Domain Module Configuration
[DependsOn(typeof(AutoCacheModule))]
public class BookStoreDomainModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Register entities
        context.Services.AddAutoCache<Book, Guid>();
        context.Services.AddAutoCache<Author, Guid>();

        // Configure options
        Configure<AutoCacheOptions>(options =>
        {
            options.EnableMetrics = true;
            options.EnableLogging = true;
            options.DefaultAbsoluteExpirationRelativeToNow = 300000; // 5 min
        });
    }
}

// 2. Entity
public class Book : AuditedAggregateRoot<Guid>
{
    public string Name { get; set; }
    public Guid AuthorId { get; set; }
    public decimal Price { get; set; }
}

// 3. Application Service
public class BookAppService : ApplicationService, IBookAppService
{
    private readonly IRepository<Book, Guid> _repository;

    [Cache(typeof(Book), Scope = AutoCacheScope.Global)]
    public virtual async Task<PagedResultDto<BookDto>> GetListAsync(
        PagedAndSortedResultRequestDto input)
    {
        var queryable = await _repository.GetQueryableAsync();
        var books = await AsyncExecuter.ToListAsync(
            queryable.Skip(input.SkipCount).Take(input.MaxResultCount)
        );

        return new PagedResultDto<BookDto>(
            await AsyncExecuter.CountAsync(queryable),
            ObjectMapper.Map<List<Book>, List<BookDto>>(books)
        );
    }

    [Cache(typeof(Book), Scope = AutoCacheScope.Entity)]
    public virtual async Task<BookDto> GetAsync(Guid id)
    {
        var book = await _repository.GetAsync(id);
        return ObjectMapper.Map<Book, BookDto>(book);
    }

    public async Task<BookDto> CreateAsync(CreateBookDto input)
    {
        var book = ObjectMapper.Map<CreateBookDto, Book>(input);
        await _repository.InsertAsync(book);
        // Cache for GetListAsync and GetAsync is automatically cleared
        return ObjectMapper.Map<Book, BookDto>(book);
    }
}
```

---

## 🎯 Summary

AutoCache simplifies caching in ABP Framework applications by:
- **Eliminating boilerplate** cache management code
- **Automating cache invalidation** based on entity changes
- **Providing flexible scoping** for different use cases
- **Integrating seamlessly** with ABP's infrastructure

By using declarative attributes, you can focus on business logic while AutoCache handles the complexity of distributed caching and invalidation.

**Next Steps**:
1. Install AutoCache in your ABP application
2. Register your entities with `AddAutoCache<TEntity>()`
3. Add `[Cache]` attributes to frequently-called methods
4. Monitor metrics to optimize cache configuration
5. Enjoy faster response times and reduced database load!

---

## 📚 Additional Resources

- [ABP Framework Documentation](https://docs.abp.io)
- [Redis Documentation](https://redis.io/docs)
- [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis)

---

**Version**: 1.0.0
**Last Updated**: 2024-01-15
**License**: MIT
