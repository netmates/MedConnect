namespace MedConnect.Shared.Events;

public sealed class IntegrationEventEnvelope<TPayload>
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public int EventVersion { get; init; } = 1;
    public DateTime OccurredAt { get; init; }
    public string? CorrelationId { get; init; }
    public string Source { get; init; } = string.Empty;
    public TPayload Payload { get; init; } = default!;
}