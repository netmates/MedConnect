namespace NotificationService.Common.Messaging;

public interface INotificationEventHandler<in TPayload>
{
    public Task HandleAsync(TPayload payload, CancellationToken ct);
}
