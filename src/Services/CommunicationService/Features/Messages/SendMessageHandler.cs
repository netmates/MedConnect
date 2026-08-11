using CommunicationService.Common.Auth;
using CommunicationService.Common.Persistence;
using MongoDB.Driver;

namespace CommunicationService.Features.Messages;

public sealed class SendMessageHandler(IMongoDatabase db)
{
    private readonly IMongoCollection<ChatDocument> _chats =
        db.GetCollection<ChatDocument>(MongoCollections.Chats);

    private readonly IMongoCollection<MessageDocument> _messages =
        db.GetCollection<MessageDocument>(MongoCollections.Messages);

    public async Task<MessageDocument?> HandleAsync(
        Guid chatId,
        SendMessageRequest request,
        string currentKeycloakId,
        string senderRole,
        CancellationToken ct)
    {
        var chat = await _chats.Find(x => x.Id == chatId).FirstOrDefaultAsync(ct);
        if (chat is null)
            return null;

        ChatAccess.EnsureParticipant(chat, currentKeycloakId);

        if (senderRole == "patient" && currentKeycloakId != chat.PatientKeycloakId)
            throw new UnauthorizedAccessException("Роль patient не совпадает с участником чата.");
        if (senderRole == "doctor" && currentKeycloakId != chat.DoctorKeycloakId)
            throw new UnauthorizedAccessException("Роль doctor не совпадает с участником чата.");

        var message = new MessageDocument
        {
            Id = Guid.NewGuid(),
            ChatId = chat.Id,
            SenderId = currentKeycloakId,
            SenderRole = senderRole,
            Text = request.Text.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _messages.InsertOneAsync(message, cancellationToken: ct);
        return message;
    }
}
