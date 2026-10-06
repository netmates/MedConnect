using System.Text.Json;
using MedConnect.Shared.Events;

namespace NotificationService.Common.Messaging;

public sealed record NotificationDeliveryResult
{
    public bool DeadLetter { get; private init; }
    public string? Reason { get; private init; }
    public Exception? Error { get; private init; }
    public Guid EventId { get; private init; }
    public string? EventType { get; private init; }
    public int EventVersion { get; private init; }
    public string? CorrelationId { get; private init; }

    public static NotificationDeliveryResult Acknowledge(IntegrationEventEnvelope<JsonElement> envelope) =>
        FromEnvelope(envelope, deadLetter: false, reason: null, error: null);

    public static NotificationDeliveryResult Reject(
        string reason,
        Exception? error = null,
        IntegrationEventEnvelope<JsonElement>? envelope = null) =>
        envelope is null
            ? new NotificationDeliveryResult
            {
                DeadLetter = true,
                Reason = reason,
                Error = error
            }
            : FromEnvelope(envelope, deadLetter: true, reason: reason, error: error);

    private static NotificationDeliveryResult FromEnvelope(
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
