using CommunicationService.Common.Auth;
using CommunicationService.Common.Exceptions;
using CommunicationService.Common.Grpc;
using CommunicationService.Common.Persistence;
using MongoDB.Driver;

namespace CommunicationService.Features.Chats;

public sealed class GetChatHistoryHandler(IMongoDatabase db, AppointmentAccessClient appointmentAccess)
{
    private readonly IMongoCollection<ChatDocument> _chats =
        db.GetCollection<ChatDocument>(MongoCollections.Chats);

    private readonly IMongoCollection<MessageDocument> _messages =
        db.GetCollection<MessageDocument>(MongoCollections.Messages);

    public async Task<IReadOnlyList<MessageDocument>> HandleAsync(
        Guid chatId,
        string currentKeycloakId,
        CancellationToken ct)
    {
        var chat = await _chats.Find(x => x.Id == chatId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Чат {chatId} не найден.");

        ChatAccess.EnsureParticipant(chat, currentKeycloakId);
        await appointmentAccess.ValidateAsync(chat.AppointmentId, currentKeycloakId, ct);

        return await _messages
            .Find(x => x.ChatId == chatId)
            .SortBy(x => x.CreatedAt)
            .ToListAsync(ct);
    }
}
