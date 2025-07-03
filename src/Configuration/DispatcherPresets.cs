using Microsoft.Extensions.Logging;

namespace DotLio.Dispatcher.Configuration;

public static class DispatcherPresets
{
    public static DispatcherOptions HighPerformance => new()
    {
        DefaultTimeout = TimeSpan.FromSeconds(5),
        EnableValidation = false,
        EnableMetrics = false,
        EnableDetailedLogging = false,
        EnableHandlerCache = true,
        MaxConcurrentNotifications = Environment.ProcessorCount * 4,
        MaxCacheSize = 10000,
        EnableTimeouts = true,
        EnableExceptionHandling = false
    };
    
    public static DispatcherOptions Development => new()
    {
        DefaultTimeout = TimeSpan.FromMinutes(5),
        EnableValidation = true,
        EnableMetrics = true,
        EnableDetailedLogging = true,
        EnableHandlerCache = true,
        MaxConcurrentNotifications = Environment.ProcessorCount,
        DefaultLogLevel = LogLevel.Debug,
        EnableTimeouts = true,
        EnableExceptionHandling = true
    };
    
    public static DispatcherOptions Production => new()
    {
        DefaultTimeout = TimeSpan.FromSeconds(30),
        EnableValidation = true,
        EnableMetrics = true,
        EnableDetailedLogging = false,
        EnableHandlerCache = true,
        MaxConcurrentNotifications = Environment.ProcessorCount * 2,
        DefaultLogLevel = LogLevel.Warning,
        EnableTimeouts = true,
        EnableExceptionHandling = true,
        EnableHealthChecks = true
    };
}