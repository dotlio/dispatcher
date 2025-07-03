namespace DotLio.Dispatcher.Diagnostics;

public class CacheStatistics
{
    public long CacheHits { get; init; }
    public long CacheMisses { get; init; }
    public double HitRate { get; init; }
    public int SingleHandlerCount { get; init; }
    public int MultiHandlerCount { get; init; }
    public int TotalCachedItems { get; init; }

    public override string ToString()
    {
        return $"Cache Stats: {CacheHits} hits, {CacheMisses} misses, {HitRate:P2} hit rate, {TotalCachedItems} cached items";
    }
}