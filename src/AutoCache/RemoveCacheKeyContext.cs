using System;
using System.Collections.Generic;

namespace AutoCache;

public class RemoveCacheKeyContext
{
    public List<Guid?>? UserIds { get; set; }

    public object?[]? Keys { get; set; }
}