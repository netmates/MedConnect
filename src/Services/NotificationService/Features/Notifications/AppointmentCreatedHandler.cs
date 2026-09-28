using MedConnect.Shared.Events;
using NotificationService.Common.Messaging;

namespace NotificationService.Features.Notifications;

public sealed class AppointmentCreatedHandler(INotificationSender sender)
    : INotificationHandler<AppointmentCreatedPayload>
{
    public async Task HandleAsync(AppointmentCreatedPayload payload, CancellationToken cancellationToken)
    {
        await sender.SendAsync(CreateMessage(payload, payload.PatientId, ParticipantRoles.Patient), cancellationToken);
        await sender.SendAsync(CreateMessage(payload, payload.DoctorId, ParticipantRoles.Doctor), cancellationToken);
    }

    private static NotificationMessage CreateMessage(
        AppointmentCreatedPayload payload,
        Guid recipientId,
        string recipientRole) =>
        new()
        {
            EventType = EventTypes.AppointmentCreated,
            RecipientId = recipientId,
            RecipientRole = recipientRole,
            AppointmentId = payload.AppointmentId,
            PatientId = payload.PatientId,
            DoctorId = payload.DoctorId,
            TextPreview = $"Appointment {payload.AppointmentId} created"
        };
}
