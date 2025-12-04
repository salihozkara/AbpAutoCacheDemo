using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities;

namespace AutoCache;

public class AutoCacheUserIdSelectorOptions
{
    private readonly Dictionary<Type, object> _currentUserScopeUserIdSelectors = new();
    private readonly Dictionary<Type, object> _currentUserScopeUserIdListSelectors = new();

    public void AddUserIdSelector<TEntity>(Func<TEntity, Guid?> userIdSelector) where TEntity : class, IEntity
    {
        _currentUserScopeUserIdSelectors[typeof(TEntity)] = userIdSelector;
    }

    public void AddUserIdListSelector<TEntity>(Func<TEntity, List<Guid?>> userIdListSelector) where TEntity : class, IEntity
    {
        _currentUserScopeUserIdListSelectors[typeof(TEntity)] = userIdListSelector;
    }

    public bool ContainsUserIdSelector(Type entityType)
    {
        return _currentUserScopeUserIdSelectors.ContainsKey(entityType) || _currentUserScopeUserIdListSelectors.ContainsKey(entityType);
    }

    public bool TryGetUserIdSelector<TEntity>(out Func<TEntity, Guid>? userIdSelector)
    {
        if (_currentUserScopeUserIdSelectors.TryGetValue(typeof(TEntity), out var selector))
        {
            userIdSelector = selector as Func<TEntity, Guid>;
            return true;
        }

        userIdSelector = null;
        return false;
    }

    public bool TryGetUserIdListSelector<TEntity>(out Func<TEntity, List<Guid?>?>? userIdListSelector)
    {
        if (_currentUserScopeUserIdListSelectors.TryGetValue(typeof(TEntity), out var selector))
        {
            userIdListSelector = selector as Func<TEntity, List<Guid?>>;
            return true;
        }

        userIdListSelector = null;
        return false;
    }
}