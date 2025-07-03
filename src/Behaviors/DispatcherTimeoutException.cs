namespace DotLio.Dispatcher.Behaviors;

public class DispatcherTimeoutException : Exception
{
    public string RequestType { get; }
    public TimeSpan Timeout { get; }

    public DispatcherTimeoutException(string requestType, TimeSpan timeout) : base($"Request {requestType} timed out after {timeout.TotalMilliseconds}ms")
    {
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        Timeout = timeout;
    }

    public DispatcherTimeoutException(string requestType, TimeSpan timeout, Exception innerException) : base($"Request {requestType} timed out after {timeout.TotalMilliseconds}ms", innerException)
    {
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        Timeout = timeout;
    }
}