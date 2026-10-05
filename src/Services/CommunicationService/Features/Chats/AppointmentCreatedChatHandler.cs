using CommunicationService.Common.Messaging;
using CommunicationService.Common.Persistence;
using MedConnect.Shared.Events;

namespace CommunicationService.Features.Chats;

public sealed class AppointmentCreatedChatHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<AppointmentCreatedChatHandler> logger) : ICommunicationEventHandler<AppointmentCreatedPayload>
{
    public Task HandleAsync(AppointmentCreatedPayload payload, CancellationToken ct) =>
        MongoConsumerRetry.ExecuteAsync(async ct =>
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
        }, logger, ct);
}
