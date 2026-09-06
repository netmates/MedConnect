using CommunicationService.Common.Grpc;
using CommunicationService.Common.Persistence;

namespace CommunicationService.Features.Chats;

public sealed class CreateChatHandler(AppointmentAccessClient appointmentAccess, EnsureChatService ensureChat)
{
    public async Task<(ChatDocument Chat, bool Created)> HandleAsync(
        Guid appointmentId,
        string currentKeycloakId,
        CancellationToken ct)
    {
        var access = await appointmentAccess.ValidateAsync(appointmentId, currentKeycloakId, ct);

        return await ensureChat.EnsureAsync(
            access.AppointmentId,
            access.PatientId,
            access.DoctorId,
            access.PatientKeycloakId,
            access.DoctorKeycloakId,
            access.PatientName,
            access.DoctorName,
            ct);
    }
}
