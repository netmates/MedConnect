using MedConnect.Shared.Events;
using Microsoft.Extensions.Options;
using NotificationService.Features.Notifications;
using NotificationService.Features.Notifications.Handlers;
using NotificationService.Features.Notifications.Senders;

namespace NotificationService.UnitTests.Notifications;

public class NotificationHandlerTests
{
    [Theory]
    [InlineData(NotificationChannels.Fake, "Notification was processed.")]
    [InlineData(NotificationChannels.Email, "Email notification would be sent.")]
    [InlineData(NotificationChannels.Sms, "SMS notification would be sent.")]
    public async Task AppointmentCreated_UsesConfiguredChannel(string channel, string messagePrefix)
    {
        // Arrange
        var (handler, logs) = CreateCreatedHandler(channel);

        // Act
        await handler.HandleAsync(CreatedPayload(), CancellationToken.None);

        // Assert
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
        // Arrange
        var payload = CreatedPayload();
        var (handler, logs) = CreateCreatedHandler(NotificationChannels.Fake);

        // Act
        await handler.HandleAsync(payload, CancellationToken.None);

        // Assert
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
        // Arrange
        var payload = CreateCancelledPayload(ParticipantRoles.Patient);
        var (sender, logs) = CreateSender(NotificationChannels.Fake);
        var handler = new AppointmentCancelledHandler(sender);

        // Act
        await handler.HandleAsync(payload, CancellationToken.None);

        // Assert
        Assert.Equal(2, logs.Entries.Count);
        Assert.All(logs.Entries, entry =>
        {
            AssertAppointmentFields(entry, payload.AppointmentId, payload.PatientId, payload.DoctorId);
            Assert.Equal(EventTypes.AppointmentCancelled, entry.Property("EventType"));
            Assert.Equal(NotificationChannels.Fake, entry.Property("Channel"));
        });
        var patient = Assert.Single(logs.Entries, entry => Equals(entry.Property("RecipientRole"), ParticipantRoles.Patient));
        var doctor = Assert.Single(logs.Entries, entry => Equals(entry.Property("RecipientRole"), ParticipantRoles.Doctor));
        Assert.Equal("Вы отменили запись к врачу Петров Петр Петрович на 15.06.2030 10:00 UTC.", patient.Property("TextPreview"));
        Assert.Equal("Пациент Иванов Иван Иванович отменил запись на 15.06.2030 10:00 UTC.", doctor.Property("TextPreview"));
    }

    [Fact]
    public async Task AppointmentCancelled_WhenDoctorCancels_UsesDoctorTexts()
    {
        // Arrange
        var payload = CreateCancelledPayload(ParticipantRoles.Doctor);
        var (sender, logs) = CreateSender(NotificationChannels.Fake);
        var handler = new AppointmentCancelledHandler(sender);

        // Act
        await handler.HandleAsync(payload, CancellationToken.None);

        // Assert
        var patient = Assert.Single(logs.Entries, entry => Equals(entry.Property("RecipientRole"), ParticipantRoles.Patient));
        var doctor = Assert.Single(logs.Entries, entry => Equals(entry.Property("RecipientRole"), ParticipantRoles.Doctor));
        Assert.Equal("Врач Петров Петр Петрович отменил запись на 15.06.2030 10:00 UTC.", patient.Property("TextPreview"));
        Assert.Equal("Вы отменили запись пациента Иванов Иван Иванович на 15.06.2030 10:00 UTC.", doctor.Property("TextPreview"));
    }

    [Fact]
    public async Task MessageCreated_LogsRecipient()
    {
        // Arrange
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

        // Act
        await handler.HandleAsync(payload, CancellationToken.None);

        // Assert
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

    private static AppointmentCancelledPayload CreateCancelledPayload(string cancelledByRole) =>
        new()
        {
            AppointmentId = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            CancelledByRole = cancelledByRole,
            StartTime = new DateTime(2030, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            PatientName = "Иванов Иван Иванович",
            DoctorName = "Петров Петр Петрович"
        };

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
