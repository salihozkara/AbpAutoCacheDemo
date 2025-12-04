namespace AutoCache;

/// <summary>
/// Statistics about cache operations
/// </summary>
public class AutoCacheStatistics
{
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public long TotalErrors { get; set; }
    public double HitRate { get; set; }
    public long TotalOperations { get; set; }
}