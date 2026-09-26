namespace MedConnect.Shared.Events;

public sealed class MessageCreatedPayload
{
    public Guid MessageId { get; init; }
    public Guid ChatId { get; init; }
    public Guid AppointmentId { get; init; }
    public Guid SenderId { get; init; }
    public string SenderRole { get; init; } = string.Empty;
    public Guid RecipientId { get; init; }
    public string RecipientRole { get; init; } = string.Empty;
    public string TextPreview { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
