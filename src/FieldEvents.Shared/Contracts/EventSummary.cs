namespace FieldEvents.Shared.Contracts;

/// <summary>What the Server pushes to Dispatcher/Technician clients over ClientsHub, and returns from the REST API.</summary>
public sealed class EventSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string Source { get; set; } = string.Empty;
    public EventPriority Priority { get; set; }
    public EventStatus Status { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
