using System.Text;
using System.Text.Json;
using MedConnect.Shared.Events;
using Serilog.Context;

namespace CommunicationService.Common.Messaging;

public static class CommunicationMessageProcessor
{
    private const int SupportedEventVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<CommunicationDeliveryResult> ProcessAsync<TPayload>(
        ReadOnlyMemory<byte> body,
        string expectedEventType,
        Func<TPayload, CancellationToken, Task> handle,
        CancellationToken ct)
    {
        IntegrationEventEnvelope<JsonElement>? envelope;
        try
        {
            var json = Encoding.UTF8.GetString(body.Span);
            envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope<JsonElement>>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            return CommunicationDeliveryResult.Reject("invalid-json", ex);
        }

        if (envelope is null)
            return CommunicationDeliveryResult.Reject("invalid-json");

        var hasPayload = envelope.Payload.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null;
        var decision = Evaluate(
            hasPayload,
            envelope.EventType,
            expectedEventType,
            envelope.EventVersion);

        if (decision.DeadLetter)
            return CommunicationDeliveryResult.Reject(decision.Reason ?? "invalid-envelope", envelope: envelope);

        TPayload? payload;
        try
        {
            payload = envelope.Payload.Deserialize<TPayload>(JsonOptions);
        }
        catch (JsonException ex)
        {
            return CommunicationDeliveryResult.Reject("invalid-payload", ex, envelope);
        }

        if (payload is null)
            return CommunicationDeliveryResult.Reject("empty-payload", envelope: envelope);

        using (LogContext.PushProperty("CorrelationId", envelope.CorrelationId))
        using (LogContext.PushProperty("EventId", envelope.EventId))
        {
            try
            {
                await handle(payload, ct);
            }
            catch (Exception ex)
            {
                return CommunicationDeliveryResult.Reject("processing-failed", ex, envelope);
            }
        }

        return CommunicationDeliveryResult.Acknowledge(envelope);
    }

    private static DeliveryDecision Evaluate(
        bool hasPayload,
        string? eventType,
        string expectedEventType,
        int eventVersion)
    {
        if (!hasPayload)
            return new DeliveryDecision(true, "empty-payload");

        if (!string.Equals(eventType, expectedEventType, StringComparison.Ordinal))
            return new DeliveryDecision(true, "unexpected-event-type");

        if (eventVersion != SupportedEventVersion)
            return new DeliveryDecision(true, "unsupported-event-version");

        return DeliveryDecision.Ack;
    }

    private readonly record struct DeliveryDecision(bool DeadLetter, string? Reason)
    {
        public static DeliveryDecision Ack { get; } = new(false, null);
    }
}
