using Microsoft.AspNetCore.SignalR;

namespace UserFlow.API.ChangesStreams.Hubs;

/// <summary>
/// SignalR hub for broadcasting entity change notifications.
/// </summary>
public class ChangeHub : Hub
{
    public async Task SubscribeToEntity(string entityName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, entityName);
    }

    public async Task UnsubscribeFromEntity(string entityName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, entityName);
    }
}