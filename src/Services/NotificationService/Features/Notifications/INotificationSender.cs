namespace NotificationService.Features.Notifications;

public interface INotificationSender
{
    public Task SendAsync(NotificationMessage message, CancellationToken ct);
}
