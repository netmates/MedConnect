namespace NotificationService.Features.Notifications.Senders;

public sealed class EmailNotificationSender(ILogger<EmailNotificationSender> logger) : LoggingNotificationSender(logger)
{
    private const string Template =
        "Email notification would be sent. Channel={Channel}, EventType={EventType}, RecipientId={RecipientId}, RecipientRole={RecipientRole}, AppointmentId={AppointmentId}, PatientId={PatientId}, DoctorId={DoctorId}, MessageId={MessageId}, TextPreview={TextPreview}";

    protected override void Log(NotificationMessage message) =>
        LogDelivery(Template, NotificationChannels.Email, message);
}
