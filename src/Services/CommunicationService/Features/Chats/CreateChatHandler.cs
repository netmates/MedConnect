using CommunicationService.Common.Grpc;
using CommunicationService.Common.Persistence;
using MongoDB.Driver;

namespace CommunicationService.Features.Chats;

public sealed class CreateChatHandler(IMongoDatabase db, AppointmentAccessClient appointmentAccess)
{
    private readonly IMongoCollection<ChatDocument> _chats = db.GetCollection<ChatDocument>(MongoCollections.Chats);

    public async Task<(ChatDocument Chat, bool Created)> HandleAsync(
        Guid appointmentId,
        string currentKeycloakId,
        CancellationToken ct)
    {
        var access = await appointmentAccess.ValidateAsync(appointmentId, currentKeycloakId, ct);

        var existing = await _chats
            .Find(x => x.AppointmentId == access.AppointmentId)
            .FirstOrDefaultAsync(ct);
        if (existing is not null) return (existing, Created: false);

        var chat = ChatDocument.Create(
            access.AppointmentId,
            access.PatientId,
            access.DoctorId,
            access.PatientKeycloakId,
            access.DoctorKeycloakId,
            access.PatientName,
            access.DoctorName);

        try
        {
            await _chats.InsertOneAsync(chat, cancellationToken: ct);
            return (chat, Created: true);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            var again = await _chats
                .Find(x => x.AppointmentId == access.AppointmentId)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException($"Чат для appointment {access.AppointmentId} не найден после DuplicateKey.");

            return (again, Created: false);
        }
    }
}
