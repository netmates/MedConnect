using MedConnect.Shared.Events;
using NotificationService.Common.Messaging;

namespace NotificationService.Features.Notifications;

public sealed class AppointmentCancelledHandler(INotificationSender sender)
    : INotificationEventHandler<AppointmentCancelledPayload>
{
    public async Task HandleAsync(AppointmentCancelledPayload payload, CancellationToken ct)
    {
        var textPreview = string.IsNullOrWhiteSpace(payload.CancelReason)
            ? "Запись отменена."
            : payload.CancelReason;

        await sender.SendAsync(CreateMessage(payload, payload.PatientId, ParticipantRoles.Patient, textPreview), ct);
        await sender.SendAsync(CreateMessage(payload, payload.DoctorId, ParticipantRoles.Doctor, textPreview), ct);
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
