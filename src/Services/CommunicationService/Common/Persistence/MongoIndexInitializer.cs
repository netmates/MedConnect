using MongoDB.Driver;

namespace CommunicationService.Common.Persistence;

public static class MongoIndexInitializer
{
    public static async Task EnsureIndexesAsync(IMongoDatabase db, CancellationToken ct = default)
    {
        var chats = db.GetCollection<ChatDocument>(MongoCollections.Chats);
        await chats.Indexes.CreateOneAsync(
            new CreateIndexModel<ChatDocument>(
                Builders<ChatDocument>.IndexKeys.Ascending(x => x.AppointmentId),
                new CreateIndexOptions { Unique = true, Name = "ux_chats_appointmentId" }),
            cancellationToken: ct);

        var messages = db.GetCollection<MessageDocument>(MongoCollections.Messages);
        await messages.Indexes.CreateOneAsync(
            new CreateIndexModel<MessageDocument>(
                Builders<MessageDocument>.IndexKeys
                    .Ascending(x => x.ChatId)
                    .Ascending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "ix_messages_chatId_createdAt" }),
            cancellationToken: ct);
    }
}
