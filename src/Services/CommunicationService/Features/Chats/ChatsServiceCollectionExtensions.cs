using CommunicationService.Common.Messaging;
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

        // RabbitMQ consumers: создать чат при записи; синхронизировать ФИО участников в чатах
        services.AddSingleton<ICommunicationEventHandler<AppointmentCreatedPayload>, AppointmentCreatedChatHandler>();
        services.AddSingleton<ICommunicationEventHandler<ParticipantNameUpdatedPayload>, SyncChatParticipantNamesHandler>();

        return services;
    }
}
