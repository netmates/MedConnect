using CommunicationService.Common.Auth;

namespace CommunicationService.Features.Chats;

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
            try
            {
                var keycloakId = CurrentUser.GetKeycloakId(http.User);
                var messages = await handler.HandleAsync(chatId, keycloakId, ct);
                if (messages is null)
                    return Results.NotFound();

                return Results.Ok(messages.Select(m => new
                {
                    id = m.Id,
                    chatId = m.ChatId,
                    senderId = m.SenderId,
                    senderRole = m.SenderRole,
                    text = m.Text,
                    createdAt = m.CreatedAt
                }));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden);
            }
        })
            .WithName("GetChatHistory")
            .WithSummary("История сообщений чата")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        return group;
    }
}
