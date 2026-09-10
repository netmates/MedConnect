namespace CommunicationService.Common.SignalR;

public static class ChatHubEvents
{
    /// <summary>
    /// Server → client: новое сообщение в чате.
    /// </summary>
    public const string ReceiveMessage = "ReceiveMessage";
}
