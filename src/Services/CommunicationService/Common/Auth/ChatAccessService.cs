using CommunicationService.Common.Exceptions;
using CommunicationService.Common.Grpc;
using CommunicationService.Common.Persistence;
using MongoDB.Driver;

namespace CommunicationService.Common.Auth;

public sealed class ChatAccessService(IMongoDatabase db, AppointmentAccessClient appointmentAccess)
{
    private readonly IMongoCollection<ChatDocument> _chats =
        db.GetCollection<ChatDocument>(MongoCollections.Chats);

    public async Task<ChatDocument> RequireOpenParticipantAsync(
        Guid chatId,
        string currentKeycloakId,
        CancellationToken ct)
    {
        var chat = await _chats.Find(x => x.Id == chatId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Чат {chatId} не найден.");

        ChatAccess.EnsureParticipant(chat, currentKeycloakId);
        await appointmentAccess.ValidateAsync(chat.AppointmentId, currentKeycloakId, ct);

        return chat;
    }
}
