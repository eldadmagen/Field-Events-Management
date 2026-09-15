using FieldEvents.Shared;

namespace FieldEvents.Server.Data.Entities;

public class EventStatusHistory
{
    public int Id { get; set; }
    public int FieldEventId { get; set; }
    public EventStatus FromStatus { get; set; }
    public EventStatus ToStatus { get; set; }
    public int? ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAtUtc { get; set; }
}
