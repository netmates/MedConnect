using CommunicationService.Common.Auth;
using CommunicationService.Common.Middleware;
using MedConnect.Shared.Messaging;
using CommunicationService.Common.Persistence;
using MedConnect.Shared.Events;
using MongoDB.Driver;

namespace CommunicationService.Features.Messages;

public sealed class SendMessageHandler(
    IMongoDatabase db,
    IIntegrationEventPublisher publisher,
    IHttpContextAccessor httpContextAccessor,
    ILogger<SendMessageHandler> logger,
    ChatAccessService chatAccess)
{
    private const int TextPreviewMaxLength = 120;

    private readonly IMongoCollection<MessageDocument> _messages =
        db.GetCollection<MessageDocument>(MongoCollections.Messages);

    public async Task<MessageDocument> HandleAsync(
        Guid chatId,
        SendMessageRequest request,
        string currentKeycloakId,
        CancellationToken ct)
    {
        var chat = await chatAccess.RequireOpenParticipantAsync(chatId, currentKeycloakId, ct);

        var senderRole = currentKeycloakId == chat.PatientKeycloakId
            ? Roles.Patient
            : Roles.Doctor;

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
