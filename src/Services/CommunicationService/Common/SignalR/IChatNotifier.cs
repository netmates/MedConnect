using CommunicationService.Features.Messages;

namespace CommunicationService.Common.SignalR;

public interface IChatNotifier
{
    Task NotifyMessageAsync(MessageResponse message, CancellationToken ct = default);
}
