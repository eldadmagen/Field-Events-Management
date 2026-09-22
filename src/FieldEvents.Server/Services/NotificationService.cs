using FieldEvents.Server.Hubs;
using FieldEvents.Shared.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace FieldEvents.Server.Services;

/// <summary>
/// Decides connected-live (SignalR) vs offline-push (stub) per user - this is "how the server knows
/// what state a user is in" from the architecture doc, backed by ConnectionManager.
/// </summary>
public class NotificationService(
    IHubContext<ClientsHub> hub,
    ConnectionManager connections,
    IPushNotificationChannel pushChannel) : INotificationService
{
    public Task NotifyDispatchersNewEventAsync(EventSummary evt, CancellationToken ct = default) =>
        hub.Clients.Group(ClientGroups.Dispatchers)
            .SendAsync(HubRoutes.NewEventReceivedMethod, evt, ct);

    public async Task NotifyTechnicianAsync(int technicianUserId, EventSummary evt, CancellationToken ct = default)
    {
        var userId = technicianUserId.ToString();

        if (connections.IsOnline(userId))
        {
            await hub.Clients.Group(ClientGroups.ForUser(userId))
                .SendAsync(HubRoutes.NewEventReceivedMethod, evt, ct);
        }
        else
        {
            await pushChannel.SendAsync(technicianUserId, evt, ct);
        }
    }

    public Task NotifyTechnicianPresenceChangedAsync(int technicianUserId, bool isOnline, CancellationToken ct = default) =>
        hub.Clients.Group(ClientGroups.Dispatchers)
            .SendAsync(HubRoutes.TechnicianPresenceChangedMethod,
                new TechnicianPresenceChanged { TechnicianId = technicianUserId, IsOnline = isOnline }, ct);
}
