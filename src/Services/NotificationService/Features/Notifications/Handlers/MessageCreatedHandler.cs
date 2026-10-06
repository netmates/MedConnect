using MedConnect.Shared.Events;
using NotificationService.Common.Messaging;
using NotificationService.Features.Notifications.Senders;

namespace NotificationService.Features.Notifications.Handlers;

public sealed class MessageCreatedHandler(INotificationSender sender)
    : INotificationEventHandler<MessageCreatedPayload>
{
    public Task HandleAsync(MessageCreatedPayload payload, CancellationToken ct) =>
        sender.SendAsync(
            new NotificationMessage
            {
                EventType = EventTypes.MessageCreated,
                RecipientId = payload.RecipientId,
                RecipientRole = payload.RecipientRole,
                AppointmentId = payload.AppointmentId,
                MessageId = payload.MessageId,
                TextPreview = payload.TextPreview
            },
            ct);
}
