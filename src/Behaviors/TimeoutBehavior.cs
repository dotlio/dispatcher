using DotLio.Dispatcher.Configuration;
using DotLio.Dispatcher.Interfaces;
using Microsoft.Extensions.Logging;

namespace DotLio.Dispatcher.Behaviors;

public class TimeoutBehavior<TRequest, TResponse>(DispatcherOptions options, ILogger<TimeoutBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly DispatcherOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<TimeoutBehavior<TRequest, TResponse>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_options.EnableTimeouts)
        {
            return await next();
        }

        var requestType = typeof(TRequest).Name;
        var timeout = GetTimeoutForRequest(request);
        
        CancellationTokenSource? timeoutCts = null;
        try
        {
            timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            _logger.LogDebug("Setting timeout of {TimeoutMs}ms for request {RequestType}",
                timeout.TotalMilliseconds, requestType);

            var result = await next().WaitAsync(timeoutCts.Token);

            _logger.LogDebug("Request {RequestType} completed within timeout", requestType);
            return result;
        }
        catch (OperationCanceledException ex) when (timeoutCts?.Token.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Request {RequestType} timed out after {TimeoutMs}ms",
                requestType, timeout.TotalMilliseconds);

            throw new DispatcherTimeoutException(requestType, timeout, ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError("Request {RequestType} timed out after {TimeoutMs}ms",
                requestType, timeout.TotalMilliseconds);

            throw new DispatcherTimeoutException(requestType, timeout, ex);
        }
        finally
        {
            timeoutCts?.Dispose();
        }
    }

    protected virtual TimeSpan GetTimeoutForRequest(TRequest request)
    {
        return request is ITimeoutRequest timeoutRequest 
            ? timeoutRequest.Timeout 
            : _options.DefaultTimeout;
    }
}

public class TimeoutBehavior<TRequest>(DispatcherOptions options, ILogger<TimeoutBehavior<TRequest>> logger) : IPipelineBehavior<TRequest> where TRequest : IRequest
{
    private readonly DispatcherOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<TimeoutBehavior<TRequest>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task Handle(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
    {
        if (!_options.EnableTimeouts)
        {
            await next();
            return;
        }

        var requestType = typeof(TRequest).Name;
        var timeout = GetTimeoutForRequest(request);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            _logger.LogDebug("Setting timeout of {TimeoutMs}ms for request {RequestType}",
                timeout.TotalMilliseconds, requestType);

            await next().WaitAsync(timeoutCts.Token);

            _logger.LogDebug("Request {RequestType} completed within timeout", requestType);
        }
        catch (OperationCanceledException ex) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Request {RequestType} timed out after {TimeoutMs}ms",
                requestType, timeout.TotalMilliseconds);

            throw new DispatcherTimeoutException(requestType, timeout, ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError("Request {RequestType} timed out after {TimeoutMs}ms",
                requestType, timeout.TotalMilliseconds);

            throw new DispatcherTimeoutException(requestType, timeout, ex);
        }
    }

    protected virtual TimeSpan GetTimeoutForRequest(TRequest request)
    {
        if (request is ITimeoutRequest timeoutRequest)
        {
            return timeoutRequest.Timeout;
        }

        return _options.DefaultTimeout;
    }
}