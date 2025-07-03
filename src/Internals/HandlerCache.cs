using System.Collections.Concurrent;
using DotLio.Dispatcher.Diagnostics;
using DotLio.Dispatcher.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotLio.Dispatcher.Internals;

public class HandlerCache(IServiceProvider serviceProvider, ILogger<HandlerCache>? logger = null) : IHandlerCache
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private readonly ConcurrentDictionary<Type, Lazy<object>> _singleHandlerCache = new();
    private readonly ConcurrentDictionary<Type, Lazy<IReadOnlyList<object>>> _multiHandlerCache = new();

    private long _cacheHits;
    private long _cacheMisses;

    public T GetHandler<T>()
    {
        var type = typeof(T);

        var lazy = _singleHandlerCache.GetOrAdd(type, CreateHandlerFactory<T>);

        try
        {
            var result = (T)lazy.Value;
            Interlocked.Increment(ref _cacheHits);
            logger?.LogTrace("Cache hit for handler {HandlerType}", type.Name);
            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error resolving handler {HandlerType}", type.Name);
            // Remove from cache to allow retry
            _singleHandlerCache.TryRemove(type, out _);
            throw;
        }
    }

    public IEnumerable<T> GetHandlers<T>()
    {
        var type = typeof(T);

        var lazy = _multiHandlerCache.GetOrAdd(type, CreateHandlersFactory<T>);

        try
        {
            var result = lazy.Value.Cast<T>();
            Interlocked.Increment(ref _cacheHits);
            logger?.LogTrace("Cache hit for handlers {HandlerType}", type.Name);
            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error resolving handlers {HandlerType}", type.Name);
            _multiHandlerCache.TryRemove(type, out _);
            throw;
        }
    }

    private Lazy<object> CreateHandlerFactory<T>(Type type)
    {
        Interlocked.Increment(ref _cacheMisses);
        logger?.LogTrace("Cache miss for handler {HandlerType}", type.Name);

        return new Lazy<object>(() =>
        {
            try
            {
                var handler = _serviceProvider.GetRequiredService<T>();
                logger?.LogDebug("Successfully resolved and cached handler {HandlerType}", type.Name);
                return handler!;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("No service"))
            {
                logger?.LogError(ex, "Handler {HandlerType} is not registered in the service container", type.Name);
                throw new InvalidOperationException($"Handler {type.Name} is not registered. Ensure it's added via AddDispatcher().", ex);
            }
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private Lazy<IReadOnlyList<object>> CreateHandlersFactory<T>(Type type)
    {
        Interlocked.Increment(ref _cacheMisses);
        logger?.LogTrace("Cache miss for handlers {HandlerType}", type.Name);

        return new Lazy<IReadOnlyList<object>>(() =>
        {
            try
            {
                var handlers = _serviceProvider.GetServices<T>().Cast<object>().ToList().AsReadOnly();
                logger?.LogDebug("Successfully resolved and cached {Count} handlers for {HandlerType}",
                    handlers.Count, type.Name);
                return handlers;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to resolve handlers {HandlerType}", type.Name);
                throw;
            }
        }, LazyThreadSafetyMode.ExecutionAndPublication);
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