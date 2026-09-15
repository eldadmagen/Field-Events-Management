using FieldEvents.Server.Domain;
using FieldEvents.Shared;

namespace FieldEvents.Server.Data.Entities;

public class FieldEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string Source { get; set; } = string.Empty;
    public EventPriority Priority { get; set; } = EventPriority.Normal;
    public EventStatus Status { get; private set; } = EventStatus.New;
    public int? AssignedTechnicianId { get; set; }
    public string? ExternalRef { get; set; }

    /// <summary>Idempotency key from the Agent's outbox - a retried send must not create a duplicate event.</summary>
    public Guid OutboxId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<EventStatusHistory> History { get; set; } = [];
    public List<EventComment> Comments { get; set; } = [];

    /// <summary>The only way to change Status - always goes through the state machine and leaves an audit trail.</summary>
    public void TransitionTo(EventStatus newStatus, int? changedByUserId)
    {
        EventStateMachine.EnsureValidTransition(Status, newStatus);

        History.Add(new EventStatusHistory
        {
            FromStatus = Status,
            ToStatus = newStatus,
            ChangedByUserId = changedByUserId,
            ChangedAtUtc = DateTimeOffset.UtcNow
        });

        Status = newStatus;
    }
}
