using CommunicationService.Common.Exceptions;
using MedConnect.Shared.Auth;
using CommunicationService.Common.Grpc;
using CommunicationService.Common.Persistence;
using MongoDB.Driver;

namespace CommunicationService.Common.Auth;

public sealed class ChatAccessService(IMongoDatabase db, AppointmentAccessClient appointmentAccess)
{
    private readonly IMongoCollection<ChatDocument> _chats =
        db.GetCollection<ChatDocument>(MongoCollections.Chats);

    private static void EnsureParticipant(ChatDocument chat, string currentKeycloakId)
    {
        if (currentKeycloakId != chat.PatientKeycloakId
            && currentKeycloakId != chat.DoctorKeycloakId)
        {
            throw new ForbiddenException("Нет доступа к этому чату.");
        }
    }

    public async Task<ChatDocument> RequireOpenParticipantAsync(
        Guid chatId,
        string currentKeycloakId,
        CancellationToken ct)
    {
        var chat = await _chats.Find(x => x.Id == chatId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Чат {chatId} не найден.");

        EnsureParticipant(chat, currentKeycloakId);
        await appointmentAccess.ValidateAsync(chat.AppointmentId, currentKeycloakId, ct);

        return chat;
    }
}
