namespace CommunicationService.Features.Hubs;

public interface IChatNotifier
{
    public Task NotifyMessageAsync(MessageResponse message, CancellationToken ct = default);
}
