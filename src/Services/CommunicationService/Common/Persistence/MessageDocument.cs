using MongoDB.Bson.Serialization.Attributes;

namespace CommunicationService.Common.Persistence;

public sealed class MessageDocument
{
    [BsonId]
    public Guid Id { get; init; }
    public Guid ChatId { get; init; }
    public string SenderId { get; init; } = null!;
    public string SenderRole { get; init; } = null!;
    public string Text { get; init; } = null!;
    public DateTime CreatedAt { get; init; }

    public static MessageDocument Create(
        Guid chatId,
        string senderId,
        string senderRole,
        string text)
    {
        return new MessageDocument
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            SenderId = senderId,
            SenderRole = senderRole,
            Text = text.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
