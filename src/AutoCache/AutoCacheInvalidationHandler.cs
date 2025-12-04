using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.Uow;

namespace AutoCache;

/// <summary>
/// Manages cache invalidation based on entity changes
/// </summary>
public class AutoCacheInvalidationHandler<TEntity> : ILocalEventHandler<EntityChangedEventData<TEntity>> where TEntity : class, IEntity
{
    private readonly IAutoCacheKeyManager _autoCacheKeyManager;
    private readonly ILogger<AutoCacheInvalidationHandler<TEntity>> _logger;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly AutoCacheUserIdSelectorOptions _userIdSelectorOptions;
    
    public AutoCacheInvalidationHandler(
        IAutoCacheKeyManager autoCacheKeyManager, 
        ILogger<AutoCacheInvalidationHandler<TEntity>> logger,
        IUnitOfWorkManager unitOfWorkManager,
        IOptions<AutoCacheUserIdSelectorOptions> userIdSelectorOptions)
    {
        _autoCacheKeyManager = autoCacheKeyManager;
        _logger = logger;
        _unitOfWorkManager = unitOfWorkManager;
        _userIdSelectorOptions = userIdSelectorOptions.Value;
    }

    public async Task HandleEventAsync(EntityChangedEventData<TEntity> eventData)
    {
        try
        {
            var entityType = typeof(TEntity);

            var context = new RemoveCacheKeyContext { Keys = eventData.Entity.GetKeys()! };
            
            if(_userIdSelectorOptions.TryGetUserIdSelector<TEntity>(out var userIdSelector))
            {
                context.UserIds = [userIdSelector!(eventData.Entity)];
            }

            if (_userIdSelectorOptions.TryGetUserIdListSelector<TEntity>(out var userIdListSelector))
            {
                var userIdList = userIdListSelector!(eventData.Entity);
                if (userIdList is { Count: > 0 })
                {
                    context.UserIds = userIdList;
                }
            }
            
            if(_unitOfWorkManager.Current != null)
            {
                _unitOfWorkManager.Current.OnCompleted(async () =>
                {
                    await _autoCacheKeyManager.RemoveCacheAndCacheKeys(entityType, context);
                });
            }
            else
            {
                await _autoCacheKeyManager.RemoveCacheAndCacheKeys(entityType, context);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(
                e, 
                "Error occurred while clearing cache for entity type {EntityType}", 
                typeof(TEntity).FullName
            );
        }
    }
}

/// <summary>
/// Manages cache invalidation based on entity changes
/// </summary>
public class AutoCacheInvalidationHandler<TEntity, TKey> : ILocalEventHandler<EntityChangedEventData<TEntity>> where TEntity : class, IEntity<TKey>
{
    private readonly IAutoCacheKeyManager _autoCacheKeyManager;
    private readonly ILogger<AutoCacheInvalidationHandler<TEntity>> _logger;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<TEntity, TKey> _repository;
    private readonly AutoCacheUserIdSelectorOptions _userIdSelectorOptions;
    
    public AutoCacheInvalidationHandler(
        IAutoCacheKeyManager autoCacheKeyManager, 
        ILogger<AutoCacheInvalidationHandler<TEntity>> logger,
        IUnitOfWorkManager unitOfWorkManager, 
        IRepository<TEntity, TKey> repository,
        IOptions<AutoCacheUserIdSelectorOptions> userIdSelectorOptions)
    {
        _autoCacheKeyManager = autoCacheKeyManager;
        _logger = logger;
        _unitOfWorkManager = unitOfWorkManager;
        _repository = repository;
        _userIdSelectorOptions = userIdSelectorOptions.Value;
    }

    public async Task HandleEventAsync(EntityChangedEventData<TEntity> eventData)
    {
        try
        {
            var entityType = typeof(TEntity);

            var context = new RemoveCacheKeyContext { Keys = eventData.Entity.GetKeys()! };
            
            if(_userIdSelectorOptions.TryGetUserIdSelector<TEntity>(out var userIdSelector))
            {
                context.UserIds = [userIdSelector!(eventData.Entity)];
            }

            if (_userIdSelectorOptions.TryGetUserIdListSelector<TEntity>(out var userIdListSelector))
            {
                var userIdList = userIdListSelector!(eventData.Entity);
                if (eventData is not EntityDeletedEventData<TEntity> && (userIdList == null || userIdList.Count == 0))
                {
                    var entity = await _repository.GetAsync(eventData.Entity.Id, includeDetails: true);
                    userIdList = userIdListSelector(entity);
                }

                if (userIdList is { Count: > 0 })
                {
                    context.UserIds = userIdList;
                }
            }
            
            if(_unitOfWorkManager.Current != null)
            {
                _unitOfWorkManager.Current.OnCompleted(async () =>
                {
                    await _autoCacheKeyManager.RemoveCacheAndCacheKeys(entityType, context);
                });
            }
            else
            {
                await _autoCacheKeyManager.RemoveCacheAndCacheKeys(entityType, context);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(
                e, 
                "Error occurred while clearing cache for entity type {EntityType}", 
                typeof(TEntity).FullName
            );
        }
    }
}