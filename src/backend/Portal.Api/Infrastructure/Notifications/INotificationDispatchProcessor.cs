namespace Portal.Application.Infrastructure.Notifications;

public interface INotificationDispatchProcessor
{
    Task ProcessPendingAsync(CancellationToken cancellationToken);
}
