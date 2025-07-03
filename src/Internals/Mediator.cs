using System.Diagnostics;
using DotLio.Dispatcher.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotLio.Dispatcher.Internals;

public class Mediator(IServiceProvider serviceProvider, IHandlerCache handlerCache, ILogger<Mediator> logger) : IMediator
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IHandlerCache _handlerCache = handlerCache ?? throw new ArgumentNullException(nameof(handlerCache));
    private readonly ILogger<Mediator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);
        await ExecuteRequest(typeof(TRequest).Name, async () =>
        {
            var handler = GetHandler<IRequestHandler<TRequest>>();
            var preProcessors = GetHandlers<IRequestPreProcessor<TRequest>>();
            var postProcessors = GetHandlers<IRequestPostProcessor<TRequest>>();
            var pipelineBehaviors = GetHandlers<IPipelineBehavior<TRequest>>();

            async Task CoreHandler()
            {
                foreach (var preProcessor in preProcessors)
                    await preProcessor.Process(request, cancellationToken);

                await handler.Handle(request, cancellationToken);

                foreach (var postProcessor in postProcessors)
                    await postProcessor.Process(request, cancellationToken);
            }

            var behaviorsList = pipelineBehaviors.ToList();

            if (behaviorsList.Count == 0)
            {
                await CoreHandler();
                return;
            }

            Func<Task> pipeline = CoreHandler;
            for (var i = behaviorsList.Count - 1; i >= 0; i--)
            {
                var behavior = behaviorsList[i];
                var next = pipeline;
                pipeline = () => behavior.Handle(request, () => next(), cancellationToken);
            }

            await pipeline();
        }, cancellationToken);
    }

    public async Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);
        return await ExecuteRequest<TResponse>(typeof(TRequest).Name, async () =>
        {
            var handler = GetHandler<IRequestHandler<TRequest, TResponse>>();
            var preProcessors = GetHandlers<IRequestPreProcessor<TRequest>>();
            var postProcessors = GetHandlers<IRequestPostProcessor<TRequest, TResponse>>();
            var pipelineBehaviors = GetHandlers<IPipelineBehavior<TRequest, TResponse>>();

            async Task<TResponse> CoreHandler()
            {
                foreach (var preProcessor in preProcessors)
                    await preProcessor.Process(request, cancellationToken);

                var response = await handler.Handle(request, cancellationToken);

                foreach (var postProcessor in postProcessors)
                    await postProcessor.Process(request, response, cancellationToken);

                return response;
            }

            var behaviorsList = pipelineBehaviors.ToList();

            if (behaviorsList.Count == 0)
            {
                return await CoreHandler();
            }

            Func<Task<TResponse>> pipeline = CoreHandler;
            for (var i = behaviorsList.Count - 1; i >= 0; i--)
            {
                var behavior = behaviorsList[i];
                var next = pipeline;
                pipeline = () => behavior.Handle(request, () => next(), cancellationToken);
            }

            return await pipeline();
        }, cancellationToken);
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        await ExecuteNotification(typeof(TNotification).Name, async () =>
        {
            var handlers = GetHandlers<INotificationHandler<TNotification>>();
            var handlersList = handlers.ToList();

            if (handlersList.Count == 0)
            {
                _logger.LogWarning("No handlers found for {NotificationType}", typeof(TNotification).Name);
                return;
            }

            _logger.LogDebug("Found {HandlerCount} handlers", handlersList.Count);

            var tasks = handlersList.Select(async handler =>
            {
                try
                {
                    await handler.Handle(notification, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Handler {HandlerType} failed for notification {NotificationType}",
                        handler.GetType().Name, typeof(TNotification).Name);
                    throw;
                }
            });

            await Task.WhenAll(tasks);
        }, cancellationToken);
    }

    private async Task ExecuteRequest(string requestType, Func<Task> operation, CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope("Request: {RequestType}", requestType);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing {RequestType}", requestType);
            await operation();
            _logger.LogInformation("{RequestType} completed in {ElapsedMs}ms", requestType, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("{RequestType} cancelled after {ElapsedMs}ms", requestType, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{RequestType} failed after {ElapsedMs}ms", requestType, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private T GetHandler<T>()
    {
        try
        {
            return _handlerCache.GetHandler<T>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Handler cache failed for {HandlerType}, falling back to service provider", typeof(T).Name);
            return _serviceProvider.GetRequiredService<T>();
        }
    }

    private IEnumerable<T> GetHandlers<T>()
    {
        try
        {
            return _handlerCache.GetHandlers<T>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Handler cache failed for {HandlerType}, falling back to service provider", typeof(T).Name);
            return _serviceProvider.GetServices<T>();
        }
    }

    private async Task<T> ExecuteRequest<T>(string requestType, Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope("Request: {RequestType}", requestType);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing {RequestType}", requestType);
            var result = await operation();
            _logger.LogInformation("{RequestType} completed in {ElapsedMs}ms", requestType, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("{RequestType} cancelled after {ElapsedMs}ms", requestType, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{RequestType} failed after {ElapsedMs}ms", requestType, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task ExecuteNotification(string notificationType, Func<Task> operation, CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope("Notification: {NotificationType}", notificationType);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Publishing {NotificationType}", notificationType);
            await operation();
            _logger.LogInformation("{NotificationType} published in {ElapsedMs}ms", notificationType, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("{NotificationType} cancelled after {ElapsedMs}ms", notificationType, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{NotificationType} failed after {ElapsedMs}ms", notificationType, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}