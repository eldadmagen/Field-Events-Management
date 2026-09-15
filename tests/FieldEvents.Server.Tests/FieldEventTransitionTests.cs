using FieldEvents.Server.Data.Entities;
using FieldEvents.Server.Domain;
using FieldEvents.Shared;
using Xunit;

namespace FieldEvents.Server.Tests;

public class FieldEventTransitionTests
{
    private static FieldEvent NewEvent() => new() { Title = "t", Description = "d", Source = "test" };

    [Fact]
    public void TransitionTo_ValidMove_UpdatesStatusAndAppendsHistory()
    {
        var evt = NewEvent();

        evt.TransitionTo(EventStatus.Assigned, changedByUserId: 7);

        Assert.Equal(EventStatus.Assigned, evt.Status);
        Assert.Single(evt.History);
        Assert.Equal(EventStatus.New, evt.History[0].FromStatus);
        Assert.Equal(EventStatus.Assigned, evt.History[0].ToStatus);
        Assert.Equal(7, evt.History[0].ChangedByUserId);
    }

    [Fact]
    public void TransitionTo_InvalidMove_ThrowsAndLeavesStatusAndHistoryUnchanged()
    {
        var evt = NewEvent();

        Assert.Throws<InvalidEventTransitionException>(() => evt.TransitionTo(EventStatus.Completed, changedByUserId: 1));

        Assert.Equal(EventStatus.New, evt.Status);
        Assert.Empty(evt.History);
    }

    [Fact]
    public void TransitionTo_MultipleValidMoves_BuildsFullHistoryInOrder()
    {
        var evt = NewEvent();

        evt.TransitionTo(EventStatus.Assigned, 1);
        evt.TransitionTo(EventStatus.InProgress, 1);
        evt.TransitionTo(EventStatus.Completed, 2);

        Assert.Equal(EventStatus.Completed, evt.Status);
        Assert.Equal(3, evt.History.Count);
        Assert.Equal([EventStatus.Assigned, EventStatus.InProgress, EventStatus.Completed],
            evt.History.Select(h => h.ToStatus));
    }
}
