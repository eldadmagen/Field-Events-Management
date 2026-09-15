using FieldEvents.Shared;

namespace FieldEvents.Server.Domain;

/// <summary>
/// Owns the FieldEvent lifecycle rules. Kept as a pure, static, DI-free class deliberately:
/// it has no side effects and no dependencies, which makes it trivial to unit test in isolation
/// and impossible to bypass accidentally (callers cannot set FieldEvent.Status directly - see FieldEvent.TransitionTo).
/// </summary>
public static class EventStateMachine
{
    private static readonly Dictionary<EventStatus, EventStatus[]> AllowedTransitions = new()
    {
        [EventStatus.New] = [EventStatus.Assigned, EventStatus.Cancelled],
        [EventStatus.Assigned] = [EventStatus.InProgress, EventStatus.Cancelled],
        [EventStatus.InProgress] = [EventStatus.Assigned, EventStatus.Completed, EventStatus.Cancelled],
        [EventStatus.Completed] = [],
        [EventStatus.Cancelled] = []
    };

    public static bool CanTransition(EventStatus from, EventStatus to) =>
        AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    /// <summary>Throws InvalidEventTransitionException when the move is not allowed.</summary>
    public static void EnsureValidTransition(EventStatus from, EventStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidEventTransitionException(from, to);
        }
    }
}
