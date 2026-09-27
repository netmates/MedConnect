namespace NotificationService.Features.Notifications;

public sealed class SmsNotificationSender(ILogger<SmsNotificationSender> logger) : LoggingNotificationSender(logger)
{
    private const string Template =
        "SMS notification would be sent. Channel={Channel}, EventType={EventType}, RecipientId={RecipientId}, RecipientRole={RecipientRole}, AppointmentId={AppointmentId}, PatientId={PatientId}, DoctorId={DoctorId}, MessageId={MessageId}, TextPreview={TextPreview}";

    protected override void Log(NotificationMessage message) =>
        LogDelivery(Template, NotificationChannels.Sms, message);
}
