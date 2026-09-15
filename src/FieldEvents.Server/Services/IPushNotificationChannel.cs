using FieldEvents.Shared.Contracts;

namespace FieldEvents.Server.Services;

/// <summary>The offline-delivery path (Web Push). See WebPushNotificationChannel for the current stub.</summary>
public interface IPushNotificationChannel
{
    Task SendAsync(int userId, EventSummary evt, CancellationToken ct = default);
}
