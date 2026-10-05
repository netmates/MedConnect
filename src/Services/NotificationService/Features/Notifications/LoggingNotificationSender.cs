namespace NotificationService.Features.Notifications;

public abstract class LoggingNotificationSender(ILogger logger) : INotificationSender
{
    public Task SendAsync(NotificationMessage message, CancellationToken ct)
    {
        Log(message);
        return Task.CompletedTask;
    }

    protected abstract void Log(NotificationMessage message);

    protected void LogDelivery(string template, string channel, NotificationMessage message)
    {
        logger.LogInformation(
            template,
            channel,
            message.EventType,
            message.RecipientId,
            message.RecipientRole,
            message.AppointmentId,
            message.PatientId,
            message.DoctorId,
            message.MessageId,
            message.TextPreview);
    }
}
