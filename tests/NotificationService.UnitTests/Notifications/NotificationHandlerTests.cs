using MedConnect.Shared.Events;
using Microsoft.Extensions.Options;
using NotificationService.Features.Notifications;

namespace NotificationService.UnitTests.Notifications;

public class NotificationHandlerTests
{
    [Theory]
    [InlineData(NotificationChannels.Fake, "Notification was processed.")]
    [InlineData(NotificationChannels.Email, "Email notification would be sent.")]
    [InlineData(NotificationChannels.Sms, "SMS notification would be sent.")]
    public async Task AppointmentCreated_UsesConfiguredChannel(string channel, string messagePrefix)
    {
        var (handler, logs) = CreateCreatedHandler(channel);

        await handler.HandleAsync(CreatedPayload(), CancellationToken.None);

        Assert.Equal(2, logs.Entries.Count);
        Assert.All(logs.Entries, entry =>
        {
            Assert.StartsWith(messagePrefix, entry.Message);
            Assert.Equal(channel, entry.Property("Channel"));
        });
    }

    [Fact]
    public async Task AppointmentCreated_LogsPatientAndDoctor()
    {
        var payload = CreatedPayload();
        var (handler, logs) = CreateCreatedHandler(NotificationChannels.Fake);

        await handler.HandleAsync(payload, CancellationToken.None);

        var patient = logs.Entries.Single(entry => Equals(entry.Property("RecipientRole"), ParticipantRoles.Patient));
        var doctor = logs.Entries.Single(entry => Equals(entry.Property("RecipientRole"), ParticipantRoles.Doctor));

        AssertAppointmentFields(patient, payload.AppointmentId, payload.PatientId, payload.DoctorId);
        AssertAppointmentFields(doctor, payload.AppointmentId, payload.PatientId, payload.DoctorId);
        Assert.Equal(payload.PatientId, patient.Property("RecipientId"));
        Assert.Equal(payload.DoctorId, doctor.Property("RecipientId"));
        Assert.Equal(EventTypes.AppointmentCreated, patient.Property("EventType"));
    }

    [Fact]
    public async Task AppointmentCancelled_LogsPatientAndDoctor()
    {
        var payload = new AppointmentCancelledPayload
        {
            AppointmentId = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            CancelReason = "Пациент отменил запись"
        };
        var (sender, logs) = CreateSender(NotificationChannels.Fake);
        var handler = new AppointmentCancelledHandler(sender);

        await handler.HandleAsync(payload, CancellationToken.None);

        Assert.Equal(2, logs.Entries.Count);
        Assert.All(logs.Entries, entry =>
        {
            AssertAppointmentFields(entry, payload.AppointmentId, payload.PatientId, payload.DoctorId);
            Assert.Equal(EventTypes.AppointmentCancelled, entry.Property("EventType"));
            Assert.Equal(payload.CancelReason, entry.Property("TextPreview"));
            Assert.Equal(NotificationChannels.Fake, entry.Property("Channel"));
        });
    }

    [Fact]
    public async Task MessageCreated_LogsRecipient()
    {
        var payload = new MessageCreatedPayload
        {
            MessageId = Guid.NewGuid(),
            AppointmentId = Guid.NewGuid(),
            RecipientId = Guid.NewGuid(),
            RecipientRole = ParticipantRoles.Doctor,
            TextPreview = "Здравствуйте"
        };
        var (sender, logs) = CreateSender(NotificationChannels.Fake);
        var handler = new MessageCreatedHandler(sender);

        await handler.HandleAsync(payload, CancellationToken.None);

        var entry = Assert.Single(logs.Entries);
        Assert.Equal(NotificationChannels.Fake, entry.Property("Channel"));
        Assert.Equal(EventTypes.MessageCreated, entry.Property("EventType"));
        Assert.Equal(payload.RecipientId, entry.Property("RecipientId"));
        Assert.Equal(payload.RecipientRole, entry.Property("RecipientRole"));
        Assert.Equal(payload.MessageId, entry.Property("MessageId"));
        Assert.Equal(payload.TextPreview, entry.Property("TextPreview"));
        Assert.Equal(payload.AppointmentId, entry.Property("AppointmentId"));
    }

    private static (AppointmentCreatedHandler Handler, CollectingLogger Logs) CreateCreatedHandler(string channel)
    {
        var (sender, logs) = CreateSender(channel);
        return (new AppointmentCreatedHandler(sender), logs);
    }

    private static (INotificationSender Sender, CollectingLogger Logs) CreateSender(string channel)
    {
        var logs = new CollectingLogger();
        var sender = new ConfiguredNotificationSender(
            Options.Create(new NotificationOptions { Channel = channel }),
            new FakeNotificationSender(new CollectingLogger<FakeNotificationSender>(logs)),
            new EmailNotificationSender(new CollectingLogger<EmailNotificationSender>(logs)),
            new SmsNotificationSender(new CollectingLogger<SmsNotificationSender>(logs)));

        return (sender, logs);
    }

    private static AppointmentCreatedPayload CreatedPayload() =>
        new()
        {
            AppointmentId = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid()
        };

    private static void AssertAppointmentFields(
        LogEntry entry,
        Guid appointmentId,
        Guid patientId,
        Guid doctorId)
    {
        Assert.Equal(appointmentId, entry.Property("AppointmentId"));
        Assert.Equal(patientId, entry.Property("PatientId"));
        Assert.Equal(doctorId, entry.Property("DoctorId"));
    }
}
