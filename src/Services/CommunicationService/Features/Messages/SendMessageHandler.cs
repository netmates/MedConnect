using CommunicationService.Common.Auth;
using CommunicationService.Common.Exceptions;
using CommunicationService.Common.Persistence;
using MongoDB.Driver;

namespace CommunicationService.Features.Messages;

public sealed class SendMessageHandler(IMongoDatabase db)
{
    private readonly IMongoCollection<ChatDocument> _chats =
        db.GetCollection<ChatDocument>(MongoCollections.Chats);

    private readonly IMongoCollection<MessageDocument> _messages =
        db.GetCollection<MessageDocument>(MongoCollections.Messages);

    public async Task<MessageDocument> HandleAsync(
        Guid chatId,
        SendMessageRequest request,
        string currentKeycloakId,
        string senderRole,
        CancellationToken ct)
    {
        var chat = await _chats.Find(x => x.Id == chatId).FirstOrDefaultAsync(ct);
        if (chat is null)
            throw new NotFoundException($"Чат {chatId} не найден.");

        ChatAccess.EnsureParticipant(chat, currentKeycloakId);

        if (senderRole == "patient" && currentKeycloakId != chat.PatientKeycloakId)
            throw new ForbiddenException("Роль patient не совпадает с участником чата.");
        if (senderRole == "doctor" && currentKeycloakId != chat.DoctorKeycloakId)
            throw new ForbiddenException("Роль doctor не совпадает с участником чата.");

        var message = MessageDocument.Create(chat.Id, currentKeycloakId, senderRole, request.Text);

        await _messages.InsertOneAsync(message, cancellationToken: ct);
        return message;
    }
}
