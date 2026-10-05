using CommunicationService.Common.Auth;
using CommunicationService.Common.Exceptions;
using CommunicationService.Common.Middleware;
using MedConnect.Messaging;
using CommunicationService.Common.Persistence;
using MedConnect.Shared.Events;
using MongoDB.Driver;

namespace CommunicationService.Features.Messages;

public sealed class SendMessageHandler(
    IMongoDatabase db,
    IIntegrationEventPublisher publisher,
    IHttpContextAccessor httpContextAccessor,
    ILogger<SendMessageHandler> logger)
{
    private const int TextPreviewMaxLength = 120;

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
        var chat = await _chats.Find(x => x.Id == chatId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Чат {chatId} не найден.");

        ChatAccess.EnsureParticipant(chat, currentKeycloakId);

        if (senderRole == Roles.Patient && currentKeycloakId != chat.PatientKeycloakId)
            throw new ForbiddenException($"Роль {Roles.Patient} не совпадает с участником чата.");
        if (senderRole == Roles.Doctor && currentKeycloakId != chat.DoctorKeycloakId)
            throw new ForbiddenException($"Роль {Roles.Doctor} не совпадает с участником чата.");

        var message = MessageDocument.Create(chat.Id, currentKeycloakId, senderRole, request.Text);

        await _messages.InsertOneAsync(message, cancellationToken: ct);

        await PublishMessageCreatedAsync(chat, message, ct);

        return message;
    }

    private async Task PublishMessageCreatedAsync(
        ChatDocument chat,
        MessageDocument message,
        CancellationToken ct)
    {
        var isPatient = message.SenderRole == Roles.Patient;
        var correlationId = httpContextAccessor.HttpContext?.Items[CorrelationIdMiddleware.ItemKey] as string;

        try
        {
            await publisher.PublishAsync(
                EventTypes.MessageCreated,
                RoutingKeys.MessageCreated,
                new MessageCreatedPayload
                {
                    MessageId = message.Id,
                    ChatId = chat.Id,
                    AppointmentId = chat.AppointmentId,
                    SenderId = isPatient ? chat.PatientId : chat.DoctorId,
                    SenderRole = isPatient ? ParticipantRoles.Patient : ParticipantRoles.Doctor,
                    RecipientId = isPatient ? chat.DoctorId : chat.PatientId,
                    RecipientRole = isPatient ? ParticipantRoles.Doctor : ParticipantRoles.Patient,
                    TextPreview = BuildTextPreview(message.Text),
                    CreatedAt = message.CreatedAt
                },
                correlationId,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to publish MessageCreated. MessageId={MessageId}, ChatId={ChatId}",
                message.Id, chat.Id);
        }
    }

    private static string BuildTextPreview(string text) =>
        text.Length <= TextPreviewMaxLength ? text : text[..TextPreviewMaxLength];
}
