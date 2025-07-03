namespace DotLio.Dispatcher.Interfaces;

public interface ITimeoutRequest
{
    TimeSpan Timeout { get; }
}