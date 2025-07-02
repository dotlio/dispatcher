namespace DotLio.Dispatcher.Interfaces;

public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

public interface IPipelineBehavior<in TRequest> where TRequest : IRequest
{
    Task Handle(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken);
}

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

public delegate Task RequestHandlerDelegate();