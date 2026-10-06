namespace NotificationService.Features.Notifications.Senders;

public interface INotificationSender
{
    public Task SendAsync(NotificationMessage message, CancellationToken ct);
}
