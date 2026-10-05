using CommunicationService.Common.Messaging;
using CommunicationService.Common.Persistence;
using MedConnect.Shared.Events;

namespace CommunicationService.Features.Chats;

public sealed class SyncChatParticipantNamesHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<SyncChatParticipantNamesHandler> logger) : ICommunicationEventHandler<ParticipantNameUpdatedPayload>
{
    public async Task HandleAsync(ParticipantNameUpdatedPayload payload, CancellationToken ct)
    {
        if (!string.Equals(payload.Role, ParticipantRoles.Patient, StringComparison.Ordinal)
            && !string.Equals(payload.Role, ParticipantRoles.Doctor, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Unknown participant role. Role={Role}, ParticipantId={ParticipantId}",
                payload.Role, payload.ParticipantId);
            return;
        }

        await MongoConsumerRetry.ExecuteAsync(async ct =>
        {
            using var scope = scopeFactory.CreateScope();
            var updater = scope.ServiceProvider.GetRequiredService<UpdateChatParticipantNamesService>();

            var modified = string.Equals(payload.Role, ParticipantRoles.Patient, StringComparison.Ordinal)
                ? await updater.UpdatePatientNameAsync(payload.ParticipantId, payload.FullName, ct)
                : await updater.UpdateDoctorNameAsync(payload.ParticipantId, payload.FullName, ct);

            logger.LogInformation(
                "ParticipantNameUpdated processed. Role={Role}, ParticipantId={ParticipantId}, ModifiedChats={ModifiedChats}",
                payload.Role, payload.ParticipantId, modified);
        }, logger, ct);
    }
}
