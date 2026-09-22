using System.Security.Claims;
using FieldEvents.Server.Services;
using FieldEvents.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FieldEvents.Server.Hubs;

public static class ClientGroups
{
    public const string Dispatchers = "Dispatchers";
    public const string Technicians = "Technicians";
    public static string ForUser(string userId) => $"user-{userId}";
}

/// <summary>Real-time channel from the Server to logged-in Dispatcher/Technician browsers (the "connected" path from the architecture doc).</summary>
[Authorize]
public class ClientsHub(ConnectionManager connections, INotificationService notifications) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);

        if (userId is not null)
        {
            var wasOnline = connections.IsOnline(userId);
            connections.AddConnection(userId, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, ClientGroups.ForUser(userId));

            if (role == nameof(UserRole.Dispatcher))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, ClientGroups.Dispatchers);
            }
            else if (role == nameof(UserRole.Technician))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, ClientGroups.Technicians);

                if (!wasOnline)
                {
                    await notifications.NotifyTechnicianPresenceChangedAsync(int.Parse(userId), true);
                }
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);

        if (userId is not null)
        {
            connections.RemoveConnection(userId, Context.ConnectionId);

            if (role == nameof(UserRole.Technician) && !connections.IsOnline(userId))
            {
                await notifications.NotifyTechnicianPresenceChangedAsync(int.Parse(userId), false);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
