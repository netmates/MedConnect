using MedConnect.Shared.Consuming;
using MedConnect.Shared.Events;

namespace CommunicationService.Features.Chats.Create;

public sealed class AppointmentCreatedChatHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<AppointmentCreatedChatHandler> logger) : IIntegrationEventHandler<AppointmentCreatedPayload>
{
    public async Task HandleAsync(AppointmentCreatedPayload payload, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var ensureChat = scope.ServiceProvider.GetRequiredService<EnsureChatService>();

        var (_, created) = await ensureChat.EnsureAsync(
            payload.AppointmentId,
            payload.PatientId,
            payload.DoctorId,
            payload.PatientKeycloakId,
            payload.DoctorKeycloakId,
            payload.PatientName,
            payload.DoctorName,
            ct);

        logger.LogInformation(
            "AppointmentCreated processed. AppointmentId={AppointmentId}, ChatCreated={ChatCreated}",
            payload.AppointmentId, created);
    }
}
