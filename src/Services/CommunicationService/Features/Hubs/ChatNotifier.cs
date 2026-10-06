using CommunicationService.Common.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace CommunicationService.Features.Hubs;

/// <summary>
/// Доставка ReceiveMessage в SignalR-группу чата.
/// </summary>
public sealed class ChatNotifier(IHubContext<ChatHub> hub) : IChatNotifier
{
    public Task NotifyMessageAsync(MessageResponse message, CancellationToken ct = default)
    {
        return hub.Clients
            .Group(ChatHubPaths.GroupName(message.ChatId))
            .SendAsync(ChatHubEvents.ReceiveMessage, message, ct);
    }
}
