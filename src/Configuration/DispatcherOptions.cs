using Microsoft.Extensions.Logging;

namespace DotLio.Dispatcher.Configuration;

public class DispatcherOptions
{
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableValidation { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableDetailedLogging { get; set; } = false;
    public int MaxConcurrentNotifications { get; set; } = Environment.ProcessorCount * 2;
    public int MaxConcurrentValidators { get; set; } = 10;
    public LogLevel DefaultLogLevel { get; set; } = LogLevel.Information;
}