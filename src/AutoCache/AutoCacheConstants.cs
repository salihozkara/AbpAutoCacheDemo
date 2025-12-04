namespace AutoCache;

/// <summary>
/// Constants used throughout the AutoCache system
/// </summary>
public static class AutoCacheConstants
{
    /// <summary>
    /// Default cache name for AutoCache wrappers
    /// </summary>
    public const string CacheName = "AutoCache";

    /// <summary>
    /// Separator used in cache key generation
    /// </summary>
    public const string KeySeparator = ":";

    /// <summary>
    /// Parameter separator in cache keys
    /// </summary>
    public const string ParameterSeparator = "_";

    /// <summary>
    /// Null parameter representation in cache keys
    /// </summary>
    public const string NullParameterValue = "null";

    /// <summary>
    /// Scope keys for different cache scopes
    /// </summary>
    public static class Scopes
    {
        public const string Anonymous = "Anonymous";
        public const string Authenticated = "Authenticated";
        public const string Unauthenticated = "Unauthenticated";
    }
}