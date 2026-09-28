namespace NotificationService.Features.Notifications;

public interface INotificationSender
{
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken);
}
