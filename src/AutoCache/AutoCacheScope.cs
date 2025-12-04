using System;

namespace AutoCache;

/// <summary>
/// Defines the scope of cache entries
/// </summary>
[Flags]
public enum AutoCacheScope
{
    /// <summary>
    /// Cache is shared globally across all users
    /// </summary>
    Global,

    /// <summary>
    /// Cache is scoped to the current user (based on user ID)
    /// </summary>
    CurrentUser,

    /// <summary>
    /// Cache is scoped to authenticated vs unauthenticated users
    /// </summary>
    AuthenticatedUser,

    /// <summary>
    /// Cache is scoped to the primary key of the entity involved
    /// </summary>
    Entity
}