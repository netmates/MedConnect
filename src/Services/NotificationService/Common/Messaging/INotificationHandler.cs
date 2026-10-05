namespace NotificationService.Common.Messaging;

public interface INotificationHandler<in TPayload>
{
    Task HandleAsync(TPayload payload, CancellationToken cancellationToken);
}
