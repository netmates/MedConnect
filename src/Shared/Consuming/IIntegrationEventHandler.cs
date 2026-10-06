namespace MedConnect.Shared.Consuming;

public interface IIntegrationEventHandler<in TPayload>
{
    public Task HandleAsync(TPayload payload, CancellationToken ct);
}
