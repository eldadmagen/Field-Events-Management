namespace FieldEvents.Shared.Contracts;

/// <summary>Payload an external source POSTs to the Agent's ingest endpoint.</summary>
public sealed class IncomingEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public EventPriority Priority { get; set; } = EventPriority.Normal;
    public string? ExternalRef { get; set; }
}
