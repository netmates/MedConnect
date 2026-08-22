using CommunicationService.Common.Exceptions;
using CommunicationService.Common.Persistence;

namespace CommunicationService.Common.Auth;

public static class ChatAccess
{
    public static void EnsureParticipant(ChatDocument chat, string currentKeycloakId)
    {
        if (currentKeycloakId != chat.PatientKeycloakId
            && currentKeycloakId != chat.DoctorKeycloakId)
        {
            throw new ForbiddenException("Нет доступа к этому чату.");
        }
    }
}
