namespace NotificationService.Features.Notifications.Senders;

public sealed class FakeNotificationSender(ILogger<FakeNotificationSender> logger) : LoggingNotificationSender(logger)
{
    private const string Template =
        "Notification was processed. Channel={Channel}, EventType={EventType}, RecipientId={RecipientId}, RecipientRole={RecipientRole}, AppointmentId={AppointmentId}, PatientId={PatientId}, DoctorId={DoctorId}, MessageId={MessageId}, TextPreview={TextPreview}";

    protected override void Log(NotificationMessage message) =>
        LogDelivery(Template, NotificationChannels.Fake, message);
}
