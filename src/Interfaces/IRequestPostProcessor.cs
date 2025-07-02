namespace DotLio.Dispatcher.Interfaces;

public interface IRequestPostProcessor<in TRequest, in TResponse> where TRequest : IRequest<TResponse>
{
    Task Process(TRequest request, TResponse response, CancellationToken cancellationToken = default);
}

public interface IRequestPostProcessor<in TRequest> where TRequest : IRequest
{
    Task Process(TRequest request, CancellationToken cancellationToken = default);
}