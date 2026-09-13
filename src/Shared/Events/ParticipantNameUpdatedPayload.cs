namespace MedConnect.Shared.Events;

public sealed class ParticipantNameUpdatedPayload
{
    public Guid ParticipantId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
}
