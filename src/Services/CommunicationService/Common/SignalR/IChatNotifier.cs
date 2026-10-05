using CommunicationService.Features.Messages;

namespace CommunicationService.Common.SignalR;

public interface IChatNotifier
{
    public Task NotifyMessageAsync(MessageResponse message, CancellationToken ct = default);
}
