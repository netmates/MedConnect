using CommunicationService.Common.Persistence;

namespace CommunicationService.Features.Messages;

public sealed record MessageResponse(
    Guid Id,
    Guid ChatId,
    string SenderId,
    string SenderRole,
    string Text,
    DateTime CreatedAt)
{
    public static MessageResponse From(MessageDocument message)
    {
        return new MessageResponse(
            message.Id,
            message.ChatId,
            message.SenderId,
            message.SenderRole,
            message.Text,
            message.CreatedAt);
    }
}
