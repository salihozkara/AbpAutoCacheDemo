using System;

namespace AutoCache;

/// <summary>
/// Wraps exceptions that occur during cache operations with additional context
/// </summary>
public class AutoCacheExceptionWrapper : Exception
{
    /// <summary>
    /// The original exception that was thrown
    /// </summary>
    public Exception OriginalException { get; }

    public AutoCacheExceptionWrapper(
        Exception originalException, 
        string? methodName = null) 
        : base($"AutoCache getOrAddAsync operation failed{(string.IsNullOrEmpty(methodName) ? "" : $" in method '{methodName}'")}: {originalException.Message}", originalException)
    {
        OriginalException = originalException;
    }
}