namespace DotLio.Dispatcher.Interfaces;

public interface IRequest
{
}

public interface IRequest<TResponse> : IRequest
{
}