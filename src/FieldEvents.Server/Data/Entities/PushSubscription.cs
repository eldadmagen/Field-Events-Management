namespace FieldEvents.Server.Data.Entities;

/// <summary>
/// Schema for the offline-notification path (Web Push/VAPID). Not wired to real delivery yet -
/// see Services/WebPushNotificationChannel.cs for the stub. Table exists so the subscribe
/// endpoint and future implementation have somewhere to persist to.
/// </summary>
public class PushSubscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
