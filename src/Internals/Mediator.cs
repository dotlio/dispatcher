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
        var pipelineBehaviors = _serviceProvider.GetServices<IPipelineBehavior<TRequest>>().Reverse().ToList();

        RequestHandlerDelegate next = () => handler.Handle(request, cancellationToken);

        foreach (var behavior in pipelineBehaviors)
        {
            var currentNext = next;
            next = () => behavior.Handle(request, currentNext, cancellationToken);
        }

        await next();
    }

    public async Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);

        var handler = _serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var pipelineBehaviors = _serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse().ToList();

        RequestHandlerDelegate<TResponse> next = () => handler.Handle(request, cancellationToken);

        foreach (var behavior in pipelineBehaviors)
        {
            var currentNext = next;
            next = () => behavior.Handle(request, currentNext, cancellationToken);
        }

        return await next();
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var handlers = _serviceProvider.GetServices<INotificationHandler<TNotification>>();
        var tasks = handlers.Select(handler => handler.Handle(notification, cancellationToken));

        await Task.WhenAll(tasks);
    }
}