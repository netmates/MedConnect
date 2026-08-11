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
}
