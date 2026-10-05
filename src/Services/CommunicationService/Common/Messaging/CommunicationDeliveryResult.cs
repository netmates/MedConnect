using System.Text.Json;
using MedConnect.Shared.Events;

namespace CommunicationService.Common.Messaging;

public sealed record CommunicationDeliveryResult
{
    public bool DeadLetter { get; private init; }
    public string? Reason { get; private init; }
    public Exception? Error { get; private init; }
    public Guid EventId { get; private init; }
    public string? EventType { get; private init; }
    public int EventVersion { get; private init; }
    public string? CorrelationId { get; private init; }

    public bool Requeue => false;

    public static CommunicationDeliveryResult Acknowledge(IntegrationEventEnvelope<JsonElement> envelope) =>
        FromEnvelope(envelope, deadLetter: false, reason: null, error: null);

    public static CommunicationDeliveryResult Reject(
        string reason,
        Exception? error = null,
        IntegrationEventEnvelope<JsonElement>? envelope = null) =>
        envelope is null
            ? new CommunicationDeliveryResult
            {
                DeadLetter = true,
                Reason = reason,
                Error = error
            }
            : FromEnvelope(envelope, deadLetter: true, reason: reason, error: error);

    private static CommunicationDeliveryResult FromEnvelope(
        IntegrationEventEnvelope<JsonElement> envelope,
        bool deadLetter,
        string? reason,
        Exception? error) =>
        new()
        {
            DeadLetter = deadLetter,
            Reason = reason,
            Error = error,
            EventId = envelope.EventId,
            EventType = envelope.EventType,
            EventVersion = envelope.EventVersion,
            CorrelationId = envelope.CorrelationId
        };
}
