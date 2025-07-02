using DotLio.Dispatcher.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DotLio.Dispatcher.Internals;

public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = _serviceProvider.GetRequiredService<IRequestHandler<TRequest>>();
        var preProcessors = GetProcessors<IRequestPreProcessor<TRequest>>();

        try
        {
            if (preProcessors.Count > 0)
            {
                foreach (var preProcessor in preProcessors)
                {
                    await preProcessor.Process(request, cancellationToken);
                }
            }

            await handler.Handle(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw;
        }
    }

    public async Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = _serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var preProcessors = GetProcessors<IRequestPreProcessor<TRequest>>();
        var postProcessors = GetProcessors<IRequestPostProcessor<TRequest, TResponse>>();
        var pipelineBehaviors = GetProcessors<IPipelineBehavior<TRequest, TResponse>>();

        try
        {
            async Task<TResponse> CoreHandler()
            {
                if (preProcessors.Count > 0)
                {
                    foreach (var preProcessor in preProcessors)
                    {
                        await preProcessor.Process(request, cancellationToken);
                    }
                }

                var response = await handler.Handle(request, cancellationToken);

                if (postProcessors.Count <= 0) return response;
                foreach (var postProcessor in postProcessors)
                {
                    await postProcessor.Process(request, response, cancellationToken);
                }

                return response;
            }

            RequestHandlerDelegate<TResponse> pipeline = CoreHandler;

            for (var i = pipelineBehaviors.Count - 1; i >= 0; i--)
            {
                var behavior = pipelineBehaviors[i];
                var currentPipeline = pipeline;
                pipeline = () => behavior.Handle(request, currentPipeline, cancellationToken);
            }

            return await pipeline();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw;
        }
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var handlers = _serviceProvider.GetServices<INotificationHandler<TNotification>>().ToList();

        if (handlers.Count == 0)
            return;

        var tasks = handlers.Select(async handler =>
        {
            try
            {
                await handler.Handle(notification, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw;
            }
        });

        await Task.WhenAll(tasks);
    }

    private List<T> GetProcessors<T>()
    {
        return _serviceProvider.GetServices<T>().ToList();
    }
}