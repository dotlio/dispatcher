using System.Collections.Concurrent;
using DotLio.Dispatcher.Diagnostics;
using DotLio.Dispatcher.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotLio.Dispatcher.Internals;

public class HandlerCache(IServiceProvider serviceProvider, ILogger<HandlerCache>? logger = null) : IHandlerCache
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private readonly ConcurrentDictionary<Type, object> _singleHandlerCache = new();
    private readonly ConcurrentDictionary<Type, object> _multiHandlerCache = new();
    
    private long _cacheHits;
    private long _cacheMisses;

    public T GetHandler<T>()
    {
        var type = typeof(T);

        if (!_singleHandlerCache.TryGetValue(type, out var cachedHandler)) return ResolveAndCacheHandler<T>(type);
        try
        {
            var result = (T)cachedHandler;
            Interlocked.Increment(ref _cacheHits);
            logger?.LogDebug("Cache hit for handler {HandlerType}", type.Name);
            return result;
        }
        catch (InvalidCastException ex)
        {
            logger?.LogWarning(ex, "Invalid cast for cached handler {HandlerType}. Clearing cache entry and resolving fresh.", type.Name);
            _singleHandlerCache.TryRemove(type, out _);
        }

        return ResolveAndCacheHandler<T>(type);
    }

    public IEnumerable<T> GetHandlers<T>()
    {
        var type = typeof(T);

        if (!_multiHandlerCache.TryGetValue(type, out var cachedHandlers)) return ResolveAndCacheHandlers<T>(type);
        try
        {
            var result = (IEnumerable<T>)cachedHandlers;
            Interlocked.Increment(ref _cacheHits);
            logger?.LogDebug("Cache hit for handlers {HandlerType}", type.Name);
            return result;
        }
        catch (InvalidCastException ex)
        {
            logger?.LogWarning(ex, "Invalid cast for cached handlers {HandlerType}. Clearing cache entry and resolving fresh.", type.Name);
            _multiHandlerCache.TryRemove(type, out _);
        }

        return ResolveAndCacheHandlers<T>(type);
    }
    
    private T ResolveAndCacheHandler<T>(Type type)
    {
        Interlocked.Increment(ref _cacheMisses);
        logger?.LogDebug("Cache miss for handler {HandlerType}", type.Name);

        try
        {
            var handler = _singleHandlerCache.GetOrAdd(type, _ =>
            {
                try
                {
                    var resolvedHandler = _serviceProvider.GetRequiredService<T>();
                    logger?.LogDebug("Successfully resolved and cached handler {HandlerType}", type.Name);
                    return resolvedHandler!;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to resolve handler {HandlerType}", type.Name);
                    throw;
                }
            });

            return (T)handler;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No service"))
        {
            logger?.LogError(ex, "Handler {HandlerType} is not registered in the service container", type.Name);
            throw new InvalidOperationException($"Handler {type.Name} is not registered. Ensure it's added via AddDispatcher().", ex);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error resolving handler {HandlerType}", type.Name);
            throw;
        }
    }
    
    private IEnumerable<T> ResolveAndCacheHandlers<T>(Type type)
    {
        Interlocked.Increment(ref _cacheMisses);
        logger?.LogDebug("Cache miss for handlers {HandlerType}", type.Name);

        try
        {
            var handlers = _multiHandlerCache.GetOrAdd(type, _ =>
            {
                try
                {
                    var resolvedHandlers = _serviceProvider.GetServices<T>().ToList();
                    logger?.LogDebug("Successfully resolved and cached {Count} handlers for {HandlerType}", 
                        resolvedHandlers.Count, type.Name);
                    return resolvedHandlers;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to resolve handlers {HandlerType}", type.Name);
                    throw;
                }
            });

            return (IEnumerable<T>)handlers;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error resolving handlers {HandlerType}", type.Name);
            throw;
        }
    }

    public void ClearCache()
    {
        var singleCount = _singleHandlerCache.Count;
        var multiCount = _multiHandlerCache.Count;
        
        _singleHandlerCache.Clear();
        _multiHandlerCache.Clear();
        
        Interlocked.Exchange(ref _cacheHits, 0);
        Interlocked.Exchange(ref _cacheMisses, 0);
        
        logger?.LogInformation("Cache cleared. Removed {SingleHandlers} single handlers and {MultiHandlers} multi handlers", 
            singleCount, multiCount);
    }

    public CacheStatistics GetStatistics()
    {
        var hits = Interlocked.Read(ref _cacheHits);
        var misses = Interlocked.Read(ref _cacheMisses);
        var totalRequests = hits + misses;
        var hitRate = totalRequests > 0 ? (double)hits / totalRequests : 0;
        
        return new CacheStatistics
        {
            CacheHits = hits,
            CacheMisses = misses,
            HitRate = hitRate,
            SingleHandlerCount = _singleHandlerCache.Count,
            MultiHandlerCount = _multiHandlerCache.Count,
            TotalCachedItems = _singleHandlerCache.Count + _multiHandlerCache.Count
        };
    }
}