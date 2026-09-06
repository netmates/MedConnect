using CommunicationService.Common.Persistence;
using MongoDB.Driver;

namespace CommunicationService.Features.Chats;

public sealed class EnsureChatService(IMongoDatabase db)
{
    private readonly IMongoCollection<ChatDocument> _chats = db.GetCollection<ChatDocument>(MongoCollections.Chats);

    public async Task<(ChatDocument Chat, bool Created)> EnsureAsync(
        Guid appointmentId,
        Guid patientId,
        Guid doctorId,
        string patientKeycloakId,
        string doctorKeycloakId,
        string patientName,
        string doctorName,
        CancellationToken ct)
    {
        var existing = await _chats
            .Find(x => x.AppointmentId == appointmentId)
            .FirstOrDefaultAsync(ct);
        if (existing is not null) return (existing, Created: false);

        var chat = ChatDocument.Create(
            appointmentId,
            patientId,
            doctorId,
            patientKeycloakId,
            doctorKeycloakId,
            patientName,
            doctorName);

        try
        {
            await _chats.InsertOneAsync(chat, cancellationToken: ct);
            return (chat, Created: true);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            var again = await _chats
                .Find(x => x.AppointmentId == appointmentId)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException($"Чат для appointment {appointmentId} не найден после DuplicateKey.");

            return (again, Created: false);
        }
    }
}
