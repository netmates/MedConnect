using System.Text;
using System.Text.Json;
using MedConnect.Shared.Events;
using Serilog.Context;

namespace NotificationService.Common.Messaging;

public static class NotificationMessageProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<NotificationDeliveryResult> ProcessAsync<TPayload>(
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
            return NotificationDeliveryResult.Reject("invalid-json", ex);
        }

        if (envelope is null)
            return NotificationDeliveryResult.Reject("invalid-json");

        var hasPayload = envelope.Payload.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null;
        var decision = NotificationDeliveryDecision.Evaluate(
            hasPayload,
            envelope.EventType,
            expectedEventType,
            envelope.EventVersion);

        if (decision.DeadLetter)
            return NotificationDeliveryResult.Reject(decision.Reason ?? "invalid-envelope", envelope: envelope);

        TPayload? payload;
        try
        {
            payload = envelope.Payload.Deserialize<TPayload>(JsonOptions);
        }
        catch (JsonException ex)
        {
            return NotificationDeliveryResult.Reject("invalid-payload", ex, envelope);
        }

        if (payload is null)
            return NotificationDeliveryResult.Reject("empty-payload", envelope: envelope);

        using (LogContext.PushProperty("CorrelationId", envelope.CorrelationId))
        using (LogContext.PushProperty("EventId", envelope.EventId))
        {
            try
            {
                await handle(payload, ct);
            }
            catch (Exception ex)
            {
                return NotificationDeliveryResult.Reject("processing-failed", ex, envelope);
            }
        }

        return NotificationDeliveryResult.Acknowledge(envelope);
    }
}
