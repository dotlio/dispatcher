using DotLio.Dispatcher.Diagnostics;

namespace DotLio.Dispatcher.Interfaces;

public interface IHandlerCache
{
    T GetHandler<T>();
    IEnumerable<T> GetHandlers<T>();
    void ClearCache();
    CacheStatistics GetStatistics();
}