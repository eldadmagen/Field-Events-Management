using FieldEvents.Shared.Contracts;

namespace FieldEvents.Server.Services;

/// <summary>
/// STUB - not implemented. Real implementation would:
///  1. Look up the user's PushSubscription row(s) (Data/Entities/PushSubscription.cs).
///  2. Build a Web Push payload (VAPID-signed) and POST it to each subscription's push service endpoint,
///     using a library such as WebPush or a hand-rolled VAPID/aes128gcm encoder.
///  3. The browser's Service Worker (client-side, not built here) receives it via a `push` event and
///     shows a notification; a `notificationclick` handler navigates to the relevant screen.
/// Left as a stub per the exercise's scope: only the "user is connected" (SignalR) path is required E2E.
/// </summary>
public class WebPushNotificationChannel(ILogger<WebPushNotificationChannel> logger) : IPushNotificationChannel
{
    public Task SendAsync(int userId, EventSummary evt, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[stub] Would send Web Push to user {UserId} for event {EventId} ('{Title}') - not implemented.",
            userId, evt.Id, evt.Title);
        return Task.CompletedTask;
    }
}
