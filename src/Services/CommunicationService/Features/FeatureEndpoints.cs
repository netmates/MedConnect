using CommunicationService.Features.Chats;
using CommunicationService.Features.Messages;
using Microsoft.AspNetCore.Authorization;

namespace CommunicationService.Features;

public static class FeatureEndpoints
{
    public static void MapFeatureEndpoints(this WebApplication app)
    {
        var chats = app.MapGroup("/api/chats")
            .RequireAuthorization(new AuthorizeAttribute
            {
                Roles = "patient,doctor"
            })
            .WithTags("Chats");

        chats.MapCreateChat();
        chats.MapGetChatHistory();
        chats.MapSendMessage();
    }
}
