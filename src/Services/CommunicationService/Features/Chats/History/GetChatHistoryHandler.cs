using CommunicationService.Common.Auth;
using CommunicationService.Common.Persistence;
using MongoDB.Driver;

namespace CommunicationService.Features.Chats.History;

public sealed class GetChatHistoryHandler(IMongoDatabase db, ChatAccessService chatAccess)
{
    private readonly IMongoCollection<MessageDocument> _messages =
        db.GetCollection<MessageDocument>(MongoCollections.Messages);

    public async Task<IReadOnlyList<MessageDocument>> HandleAsync(
        Guid chatId,
        string currentKeycloakId,
        CancellationToken ct)
    {
        var chat = await chatAccess.RequireOpenParticipantAsync(chatId, currentKeycloakId, ct);

        return await _messages
            .Find(x => x.ChatId == chat.Id)
            .SortBy(x => x.CreatedAt)
            .ToListAsync(ct);
    }
}
