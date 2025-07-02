using DotLio.Dispatcher.Interfaces;

namespace DotLio.Dispatcher.Internals;

public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var handlerType = typeof(IRequestHandler<>).MakeGenericType(request.GetType());
        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null) throw new InvalidOperationException($"No handler for {request.GetType().Name}");
        await ((dynamic)handler).Handle(request, cancellationToken);
    }

    public async Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse>
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null) throw new InvalidOperationException($"No handler for {request.GetType().Name}");
        return await ((dynamic)handler).Handle(request, cancellationToken);       
    }
}