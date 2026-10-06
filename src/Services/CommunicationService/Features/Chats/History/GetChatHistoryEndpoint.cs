using CommunicationService.Common.Auth;

namespace CommunicationService.Features.Chats.History;

public static class GetChatHistoryEndpoint
{
    public static RouteGroupBuilder MapGetChatHistory(this RouteGroupBuilder group)
    {
        group.MapGet("/{chatId:guid}/messages", async (
                Guid chatId,
                GetChatHistoryHandler handler,
                HttpContext http,
                CancellationToken ct) =>
        {
            var keycloakId = CurrentUser.GetKeycloakId(http.User);
            var messages = await handler.HandleAsync(chatId, keycloakId, ct);

            var body = messages.Select(m => MessageResponse.From(m));

            return Results.Ok(body);
        })
            .WithName("GetChatHistory")
            .WithSummary("История сообщений чата")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status503ServiceUnavailable);

        return group;
    }
}
