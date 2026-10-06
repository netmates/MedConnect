using CommunicationService.Common.Auth;
using CommunicationService.Features.Chats.Create;
using CommunicationService.Features.Chats.History;
using CommunicationService.Features.Chats.ParticipantNames;
using MedConnect.Shared.Consuming;
using MedConnect.Shared.Events;

namespace CommunicationService.Features.Chats;

public static class ChatsServiceCollectionExtensions
{
    public static IServiceCollection AddChatFeatures(this IServiceCollection services)
    {
        // CreateChat: создать чат по appointment (идемпотентно)
        services.AddScoped<CreateChatHandler>();
        // GetChatHistory: история сообщений для участника чата
        services.AddScoped<GetChatHistoryHandler>();
        // Общий Mongo ensure-chat (HTTP CreateChat + AppointmentCreated consumer)
        services.AddScoped<EnsureChatService>();
        // Sync ФИО участников чата из RabbitMQ (ParticipantNameUpdated)
        services.AddScoped<UpdateChatParticipantNamesService>();
        // История, отправка и JoinChat: участник чата, запись еще открыта
        services.AddScoped<ChatAccessService>();

        // RabbitMQ consumers: создать чат при записи; синхронизировать ФИО участников в чатах
        services.AddSingleton<IIntegrationEventHandler<AppointmentCreatedPayload>, AppointmentCreatedChatHandler>();
        services.AddSingleton<IIntegrationEventHandler<ParticipantNameUpdatedPayload>, ParticipantNameUpdatedChatHandler>();

        return services;
    }
}
