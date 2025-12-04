using System;

namespace AutoCache;

public class AddCacheKeyContext
{
    public AutoCacheScope Scope { get; set; }
    public Guid? UserId { get; set; }

    public object?[]? Keys { get; set; }
}