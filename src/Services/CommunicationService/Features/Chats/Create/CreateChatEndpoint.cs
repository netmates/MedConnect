using MedConnect.Shared.Auth;
using FluentValidation;

namespace CommunicationService.Features.Chats.Create;

public static class CreateChatEndpoint
{
    public static RouteGroupBuilder MapCreateChat(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
                CreateChatRequest request,
                CreateChatHandler handler,
                IValidator<CreateChatRequest> validator,
                HttpContext http,
                CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(request, ct);

            var keycloakId = CurrentUser.GetKeycloakId(http.User);
            var (chat, created) = await handler.HandleAsync(request.AppointmentId, keycloakId, ct);

            var body = CreateChatResponse.From(chat);

            return created
                ? Results.Created($"/api/chats/{chat.Id}/messages", body)
                : Results.Ok(body);
        })
            .WithName("CreateChat")
            .WithSummary("Создать чат по appointment")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status503ServiceUnavailable);

        return group;
    }
}
