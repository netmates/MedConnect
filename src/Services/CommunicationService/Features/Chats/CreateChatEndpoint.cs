using CommunicationService.Common.Auth;
using FluentValidation;

namespace CommunicationService.Features.Chats;

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
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);

            var keycloakId = CurrentUser.GetKeycloakId(http.User);
            var (chat, created) = await handler.HandleAsync(request, keycloakId, ct);

            var body = CreateChatResponse.From(chat);

            return created
                ? Results.Created($"/api/chats/{chat.Id}", body)
                : Results.Ok(body);
        })
            .WithName("CreateChat")
            .WithSummary("Создать чат по appointment")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status403Forbidden);

        return group;
    }
}
