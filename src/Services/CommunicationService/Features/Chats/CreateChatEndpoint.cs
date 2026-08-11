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
                return Results.ValidationProblem(validation.ToDictionary());

            try
            {
                var keycloakId = CurrentUser.GetKeycloakId(http.User);
                var (chat, created) = await handler.HandleAsync(request, keycloakId, ct);

                var body = ToResponse(chat);
                return created
                    ? Results.Created($"/api/chats/{chat.Id}", body)
                    : Results.Ok(body);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden);
            }
        })
            .WithName("CreateChat")
            .WithSummary("Создать чат по appointment")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status403Forbidden);

        return group;
    }

    private static object ToResponse(Common.Persistence.ChatDocument chat) => new
    {
        id = chat.Id,
        appointmentId = chat.AppointmentId,
        patientId = chat.PatientId,
        doctorId = chat.DoctorId,
        patientName = chat.PatientName,
        doctorName = chat.DoctorName,
        createdAt = chat.CreatedAt
    };
}
