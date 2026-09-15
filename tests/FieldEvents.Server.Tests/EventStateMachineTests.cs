using FieldEvents.Server.Domain;
using FieldEvents.Shared;
using Xunit;

namespace FieldEvents.Server.Tests;

public class EventStateMachineTests
{
    [Theory]
    [InlineData(EventStatus.New, EventStatus.Assigned)]
    [InlineData(EventStatus.New, EventStatus.Cancelled)]
    [InlineData(EventStatus.Assigned, EventStatus.InProgress)]
    [InlineData(EventStatus.Assigned, EventStatus.Cancelled)]
    [InlineData(EventStatus.InProgress, EventStatus.Completed)]
    [InlineData(EventStatus.InProgress, EventStatus.Cancelled)]
    [InlineData(EventStatus.InProgress, EventStatus.Assigned)]
    public void CanTransition_AllowsDefinedTransitions(EventStatus from, EventStatus to)
    {
        Assert.True(EventStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(EventStatus.New, EventStatus.InProgress)]
    [InlineData(EventStatus.New, EventStatus.Completed)]
    [InlineData(EventStatus.Assigned, EventStatus.Completed)]
    [InlineData(EventStatus.Completed, EventStatus.Cancelled)]
    [InlineData(EventStatus.Completed, EventStatus.New)]
    [InlineData(EventStatus.Cancelled, EventStatus.New)]
    [InlineData(EventStatus.Cancelled, EventStatus.InProgress)]
    public void CanTransition_RejectsUndefinedTransitions(EventStatus from, EventStatus to)
    {
        Assert.False(EventStateMachine.CanTransition(from, to));
    }

    [Fact]
    public void EnsureValidTransition_ThrowsWithFromAndToOnInvalidMove()
    {
        var ex = Assert.Throws<InvalidEventTransitionException>(
            () => EventStateMachine.EnsureValidTransition(EventStatus.Completed, EventStatus.InProgress));

        Assert.Equal(EventStatus.Completed, ex.From);
        Assert.Equal(EventStatus.InProgress, ex.To);
    }

    [Fact]
    public void EnsureValidTransition_DoesNotThrowOnValidMove()
    {
        var exception = Record.Exception(
            () => EventStateMachine.EnsureValidTransition(EventStatus.New, EventStatus.Assigned));

        Assert.Null(exception);
    }

    [Fact]
    public void EveryStatus_HasNoTransitionToItself()
    {
        foreach (EventStatus status in Enum.GetValues<EventStatus>())
        {
            Assert.False(EventStateMachine.CanTransition(status, status), $"{status} should not transition to itself.");
        }
    }
}
