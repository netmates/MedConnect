namespace CommunicationService.Common.Messaging;

public interface ICommunicationEventHandler<in TPayload>
{
    public Task HandleAsync(TPayload payload, CancellationToken ct);
}
