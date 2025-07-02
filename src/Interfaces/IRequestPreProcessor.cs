namespace DotLio.Dispatcher.Interfaces;

public interface IRequestPreProcessor<in TRequest> where TRequest : IRequest
{
    Task Process(TRequest request, CancellationToken cancellationToken = default);
}