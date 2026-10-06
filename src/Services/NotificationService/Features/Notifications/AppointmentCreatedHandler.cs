using MedConnect.Shared.Events;
using NotificationService.Common.Messaging;

namespace NotificationService.Features.Notifications;

public sealed class AppointmentCreatedHandler(INotificationSender sender)
    : INotificationEventHandler<AppointmentCreatedPayload>
{
    public async Task HandleAsync(AppointmentCreatedPayload payload, CancellationToken ct)
    {
        var when = payload.StartTime.ToString("dd.MM.yyyy HH:mm");

        await sender.SendAsync(
            CreateMessage(payload, payload.PatientId, ParticipantRoles.Patient,
                $"Вы записаны к врачу {payload.DoctorName} на {when} UTC."),
            ct);

        await sender.SendAsync(
            CreateMessage(payload, payload.DoctorId, ParticipantRoles.Doctor,
                $"Пациент {payload.PatientName} записался на {when} UTC."),
            ct);
    }

    private static NotificationMessage CreateMessage(
        AppointmentCreatedPayload payload,
        Guid recipientId,
        string recipientRole,
        string textPreview) => new()
        {
            EventType = EventTypes.AppointmentCreated,
            RecipientId = recipientId,
            RecipientRole = recipientRole,
            AppointmentId = payload.AppointmentId,
            PatientId = payload.PatientId,
            DoctorId = payload.DoctorId,
            TextPreview = textPreview
        };
}
