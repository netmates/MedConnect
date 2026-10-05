namespace NotificationService.Common.Messaging;

public interface INotificationHandler<in TPayload>
{
    public Task HandleAsync(TPayload payload, CancellationToken cancellationToken);
}
