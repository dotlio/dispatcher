namespace DotLio.Dispatcher.Interfaces;

public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken = default);
}