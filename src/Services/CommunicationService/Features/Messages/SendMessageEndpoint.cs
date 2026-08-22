using CommunicationService.Common.Auth;
using FluentValidation;

namespace CommunicationService.Features.Messages;

public static class SendMessageEndpoint
{
    public static RouteGroupBuilder MapSendMessage(this RouteGroupBuilder group)
    {
        group.MapPost("/{chatId:guid}/messages", async (
                Guid chatId,
                SendMessageRequest request,
                SendMessageHandler handler,
                IValidator<SendMessageRequest> validator,
                HttpContext http,
                CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);

            var keycloakId = CurrentUser.GetKeycloakId(http.User);
            var role = CurrentUser.GetSenderRole(http.User);
            var message = await handler.HandleAsync(chatId, request, keycloakId, role, ct);

            var body = MessageResponse.From(message);

            return Results.Created($"/api/chats/{chatId}/messages", body);
        })
            .WithName("SendMessage")
            .WithSummary("Отправить сообщение в чат")
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        return group;
    }
}
