namespace DotLio.Dispatcher.Interfaces;

public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    Task Handle(INotification notification, CancellationToken cancellationToken = default);   
}