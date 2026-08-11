using CommunicationService.Common.Persistence;
using MongoDB.Driver;

namespace CommunicationService.Features.Chats;

public sealed class CreateChatHandler(IMongoDatabase db)
{
    private readonly IMongoCollection<ChatDocument> _chats =
        db.GetCollection<ChatDocument>(MongoCollections.Chats);

    public async Task<(ChatDocument Chat, bool Created)> HandleAsync(
        CreateChatRequest request,
        string currentKeycloakId,
        CancellationToken ct)
    {
        if (currentKeycloakId != request.PatientKeycloakId
            && currentKeycloakId != request.DoctorKeycloakId)
        {
            throw new UnauthorizedAccessException(
                "Создать чат может только пациент или врач этой записи.");
        }

        var existing = await _chats
            .Find(x => x.AppointmentId == request.AppointmentId)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return (existing, Created: false);

        var chat = ChatDocument.Create(
            request.AppointmentId,
            request.PatientId,
            request.DoctorId,
            request.PatientKeycloakId,
            request.DoctorKeycloakId,
            request.PatientName,
            request.DoctorName);

        try
        {
            await _chats.InsertOneAsync(chat, cancellationToken: ct);
            return (chat, Created: true);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            var again = await _chats
                .Find(x => x.AppointmentId == request.AppointmentId)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException(
                    $"Чат для appointment {request.AppointmentId} не найден после DuplicateKey.");

            return (again, Created: false);
        }
    }
}
