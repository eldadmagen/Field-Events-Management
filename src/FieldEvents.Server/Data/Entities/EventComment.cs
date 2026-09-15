namespace FieldEvents.Server.Data.Entities;

/// <summary>A technician's note/update to the dispatcher on an active event.</summary>
public class EventComment
{
    public int Id { get; set; }
    public int FieldEventId { get; set; }
    public int UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
