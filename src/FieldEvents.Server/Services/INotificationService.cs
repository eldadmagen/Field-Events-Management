using FieldEvents.Shared.Contracts;

namespace FieldEvents.Server.Services;

public interface INotificationService
{
    /// <summary>Broadcasts a new event to every currently-connected dispatcher. Implements step 4-5 of the required E2E flow.</summary>
    Task NotifyDispatchersNewEventAsync(EventSummary evt, CancellationToken ct = default);

    /// <summary>
    /// Skeleton for the "assign to technician" flow: online -> SignalR, offline -> push (stub).
    /// Not exercised by the required E2E flow, but wired end-to-end at the interface/plumbing level.
    /// </summary>
    Task NotifyTechnicianAsync(int technicianUserId, EventSummary evt, CancellationToken ct = default);

    /// <summary>Broadcasts a technician's connect/disconnect to every currently-connected dispatcher, for the technician status dashboard.</summary>
    Task NotifyTechnicianPresenceChangedAsync(int technicianUserId, bool isOnline, CancellationToken ct = default);
}
