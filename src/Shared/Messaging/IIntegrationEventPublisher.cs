namespace MedConnect.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TPayload>(
        string eventType,
        string routingKey,
        TPayload payload,
        string? correlationId,
        CancellationToken ct);
}
