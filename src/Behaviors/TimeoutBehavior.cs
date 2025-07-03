using DotLio.Dispatcher.Configuration;
using DotLio.Dispatcher.Interfaces;
using Microsoft.Extensions.Logging;

namespace DotLio.Dispatcher.Behaviors;

public class TimeoutBehavior<TRequest, TResponse>(DispatcherOptions options, ILogger<TimeoutBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(options.DefaultTimeout);

        try
        {
            return await next().WaitAsync(timeoutCts.Token);
        }
        catch (TimeoutException)
        {
            var requestType = typeof(TRequest).Name;
            logger.LogError("Request {RequestType} timed out after {TimeoutMs}ms", requestType, options.DefaultTimeout.TotalMilliseconds);
            throw new DispatcherTimeoutException(requestType, options.DefaultTimeout);
        }
    }
}

public class DispatcherTimeoutException(string requestType, TimeSpan timeout) : Exception($"Request {requestType} timed out after {timeout.TotalMilliseconds}ms")
{
    public string RequestType { get; } = requestType;
    public TimeSpan Timeout { get; } = timeout;
}