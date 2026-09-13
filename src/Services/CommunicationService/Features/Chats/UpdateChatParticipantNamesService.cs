using CommunicationService.Common.Persistence;
using MongoDB.Driver;

namespace CommunicationService.Features.Chats;

public sealed class UpdateChatParticipantNamesService(IMongoDatabase db)
{
    private readonly IMongoCollection<ChatDocument> _chats = db.GetCollection<ChatDocument>(MongoCollections.Chats);

    public async Task<long> UpdatePatientNameAsync(Guid patientId, string fullName, CancellationToken ct)
    {
        var filter = Builders<ChatDocument>.Filter.Eq(x => x.PatientId, patientId);
        var update = Builders<ChatDocument>.Update.Set(x => x.PatientName, fullName);
        var result = await _chats.UpdateManyAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount;
    }

    public async Task<long> UpdateDoctorNameAsync(Guid doctorId, string fullName, CancellationToken ct)
    {
        var filter = Builders<ChatDocument>.Filter.Eq(x => x.DoctorId, doctorId);
        var update = Builders<ChatDocument>.Update.Set(x => x.DoctorName, fullName);
        var result = await _chats.UpdateManyAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount;
    }
}
