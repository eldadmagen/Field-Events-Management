namespace FieldEvents.Shared.Contracts;

/// <summary>Ack the Server hub method returns for an AgentEventMessage, used by the Agent to mark its outbox row as sent.</summary>
public sealed class EventAckResponse
{
    public Guid OutboxId { get; set; }
    public bool Success { get; set; }
    public int? ServerEventId { get; set; }
    public string? Error { get; set; }
}
