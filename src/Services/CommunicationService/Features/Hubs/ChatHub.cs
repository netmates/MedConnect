using CommunicationService.Common.Auth;
using CommunicationService.Common.Exceptions;
using CommunicationService.Common.Persistence;
using CommunicationService.Common.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace CommunicationService.Features.Hubs;

/// <summary>
/// Real-time чат: Join/Leave.
/// </summary>
[Authorize(Roles = Roles.PatientOrDoctor)]
public sealed class ChatHub(IMongoDatabase db) : Hub
{
    private readonly IMongoCollection<ChatDocument> _chats = db.GetCollection<ChatDocument>(MongoCollections.Chats);

    /// <summary>
    /// Войти в группу чата (только участник).
    /// </summary>
    public async Task JoinChat(Guid chatId)
    {
        try
        {
            var keycloakId = CurrentUser.GetKeycloakId(Context.User!);
            var chat = await _chats.Find(x => x.Id == chatId)
                .FirstOrDefaultAsync(Context.ConnectionAborted)
                ?? throw new NotFoundException($"Чат {chatId} не найден.");

            ChatAccess.EnsureParticipant(chat, keycloakId);

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                ChatHubPaths.GroupName(chatId),
                Context.ConnectionAborted);
        }
        catch (NotFoundException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (ForbiddenException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    /// <summary>
    /// Выйти из группы чата.
    /// </summary>
    public Task LeaveChat(Guid chatId)
    {
        return Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            ChatHubPaths.GroupName(chatId),
            Context.ConnectionAborted);
    }
}
