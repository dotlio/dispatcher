using Microsoft.Extensions.Logging;

namespace DotLio.Dispatcher.Configuration;

public class DispatcherOptions
{
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableValidation { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableDetailedLogging { get; set; } = false;
    public bool EnableHandlerCache { get; set; } = true;
    public int MaxConcurrentNotifications { get; set; } = Environment.ProcessorCount * 2;
    public int MaxConcurrentValidators { get; set; } = 10;
    public LogLevel DefaultLogLevel { get; set; } = LogLevel.Information;
    public bool EnableTimeouts { get; set; } = true;
    public bool EnableExceptionHandling { get; set; } = true;
    public int MaxCacheSize { get; set; } = 1000;
    public TimeSpan CacheCleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableHealthChecks { get; set; } = true;

    public void Validate()
    {
        if (DefaultTimeout <= TimeSpan.Zero)
            throw new ArgumentException("DefaultTimeout must be greater than zero", nameof(DefaultTimeout));

        if (MaxConcurrentNotifications <= 0)
            throw new ArgumentException("MaxConcurrentNotifications must be greater than zero", nameof(MaxConcurrentNotifications));

        if (MaxConcurrentValidators <= 0)
            throw new ArgumentException("MaxConcurrentValidators must be greater than zero", nameof(MaxConcurrentValidators));

        if (MaxCacheSize < 0)
            throw new ArgumentException("MaxCacheSize must be zero or greater", nameof(MaxCacheSize));

        if (CacheCleanupInterval <= TimeSpan.Zero)
            throw new ArgumentException("CacheCleanupInterval must be greater than zero", nameof(CacheCleanupInterval));
    }

    public DispatcherOptions Clone()
    {
        return new DispatcherOptions
        {
            DefaultTimeout = DefaultTimeout,
            EnableValidation = EnableValidation,
            EnableMetrics = EnableMetrics,
            EnableDetailedLogging = EnableDetailedLogging,
            EnableHandlerCache = EnableHandlerCache,
            MaxConcurrentNotifications = MaxConcurrentNotifications,
            MaxConcurrentValidators = MaxConcurrentValidators,
            DefaultLogLevel = DefaultLogLevel,
            EnableTimeouts = EnableTimeouts,
            EnableExceptionHandling = EnableExceptionHandling,
            MaxCacheSize = MaxCacheSize,
            CacheCleanupInterval = CacheCleanupInterval,
            EnableHealthChecks = EnableHealthChecks
        };
    }

    public override string ToString()
    {
        return $"""
                DispatcherOptions:
                - DefaultTimeout: {DefaultTimeout}
                - EnableValidation: {EnableValidation}
                - EnableMetrics: {EnableMetrics}
                - EnableDetailedLogging: {EnableDetailedLogging}
                - EnableHandlerCache: {EnableHandlerCache}
                - MaxConcurrentNotifications: {MaxConcurrentNotifications}
                - MaxConcurrentValidators: {MaxConcurrentValidators}
                - DefaultLogLevel: {DefaultLogLevel}
                - EnableTimeouts: {EnableTimeouts}
                - EnableExceptionHandling: {EnableExceptionHandling}
                - MaxCacheSize: {MaxCacheSize}
                - CacheCleanupInterval: {CacheCleanupInterval}
                - EnableHealthChecks: {EnableHealthChecks}
                """;
    }
}