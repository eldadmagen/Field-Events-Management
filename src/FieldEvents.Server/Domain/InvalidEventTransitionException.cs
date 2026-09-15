using FieldEvents.Shared;

namespace FieldEvents.Server.Domain;

public sealed class InvalidEventTransitionException : Exception
{
    public EventStatus From { get; }
    public EventStatus To { get; }

    public InvalidEventTransitionException(EventStatus from, EventStatus to)
        : base($"Cannot transition event from '{from}' to '{to}'.")
    {
        From = from;
        To = to;
    }
}
