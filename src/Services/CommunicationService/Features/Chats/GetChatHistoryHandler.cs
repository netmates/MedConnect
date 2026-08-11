using CommunicationService.Common.Auth;
using CommunicationService.Common.Persistence;
using MongoDB.Driver;

namespace CommunicationService.Features.Chats;

public sealed class GetChatHistoryHandler(IMongoDatabase db)
{
    private readonly IMongoCollection<ChatDocument> _chats =
        db.GetCollection<ChatDocument>(MongoCollections.Chats);

    private readonly IMongoCollection<MessageDocument> _messages =
        db.GetCollection<MessageDocument>(MongoCollections.Messages);

    public async Task<IReadOnlyList<MessageDocument>?> HandleAsync(
        Guid chatId,
        string currentKeycloakId,
        CancellationToken ct)
    {
        var chat = await _chats.Find(x => x.Id == chatId).FirstOrDefaultAsync(ct);
        if (chat is null)
            return null;

        ChatAccess.EnsureParticipant(chat, currentKeycloakId);

        return await _messages
            .Find(x => x.ChatId == chatId)
            .SortBy(x => x.CreatedAt)
            .ToListAsync(ct);
    }
}
