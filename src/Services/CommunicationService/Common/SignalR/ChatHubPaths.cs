namespace CommunicationService.Common.SignalR;

public static class ChatHubPaths
{
    /// <summary>
    /// Endpoint SignalR ChatHub.
    /// </summary>
    public const string Hub = "/api/chats/hub";

    /// <summary>
    /// Имя SignalR-группы для чата (Join / ReceiveMessage).
    /// </summary>
    public static string GroupName(Guid chatId) => chatId.ToString("D");
}
