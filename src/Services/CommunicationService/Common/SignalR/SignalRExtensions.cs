using CommunicationService.Features.Hubs;

namespace CommunicationService.Common.SignalR;

public static class SignalRExtensions
{
    /// <summary>
    /// SignalR: сервисы хаба + notifier для ReceiveMessage.
    /// </summary>
    public static IServiceCollection AddChatSignalR(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IChatNotifier, ChatNotifier>();
        return services;
    }

    /// <summary>
    /// SignalR ChatHub на /api/chats/hub (JWT: Authorization или query access_token).
    /// </summary>
    public static WebApplication MapChatHub(this WebApplication app)
    {
        app.MapHub<ChatHub>(ChatHubPaths.Hub);
        return app;
    }
}
