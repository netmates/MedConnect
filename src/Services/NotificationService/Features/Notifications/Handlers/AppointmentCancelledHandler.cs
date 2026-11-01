using MedConnect.Shared.Consuming;
using MedConnect.Shared.Events;
using NotificationService.Features.Notifications.Senders;

namespace NotificationService.Features.Notifications.Handlers;

public sealed class AppointmentCancelledHandler(INotificationSender sender)
    : IIntegrationEventHandler<AppointmentCancelledPayload>
{
    public async Task HandleAsync(AppointmentCancelledPayload payload, CancellationToken ct)
    {
        var when = payload.StartTime.ToString("dd.MM.yyyy HH:mm");
        var cancelledByPatient = payload.CancelledByRole == ParticipantRoles.Patient;

        var patientText = cancelledByPatient
            ? $"Вы отменили запись к врачу {payload.DoctorName} на {when} UTC."
            : $"Врач {payload.DoctorName} отменил запись на {when} UTC.";

        var doctorText = cancelledByPatient
            ? $"Пациент {payload.PatientName} отменил запись на {when} UTC."
            : $"Вы отменили запись пациента {payload.PatientName} на {when} UTC.";

        await sender.SendAsync(CreateMessage(payload, payload.PatientId, ParticipantRoles.Patient, patientText), ct);
        await sender.SendAsync(CreateMessage(payload, payload.DoctorId, ParticipantRoles.Doctor, doctorText), ct);
    }

    private static NotificationMessage CreateMessage(
        AppointmentCancelledPayload payload,
        Guid recipientId,
        string recipientRole,
        string textPreview) => new()
        {
            EventType = EventTypes.AppointmentCancelled,
            RecipientId = recipientId,
            RecipientRole = recipientRole,
            AppointmentId = payload.AppointmentId,
            PatientId = payload.PatientId,
            DoctorId = payload.DoctorId,
            TextPreview = textPreview
        };
}
