using System.Security.Claims;
using FieldEvents.Server.Data;
using FieldEvents.Server.Data.Entities;
using FieldEvents.Server.Services;
using FieldEvents.Shared;
using FieldEvents.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldEvents.Server.Controllers;

public record AssignRequest(int TechnicianId);
public record StatusChangeRequest(EventStatus NewStatus);
public record CommentRequest(string Text);
public record PriorityChangeRequest(EventPriority NewPriority);

/// <summary>
/// REST surface for everything the required E2E flow doesn't need over SignalR (reads, and the
/// dispatcher/technician actions). Assign/transfer/comment are functional (not just stubs) since
/// they follow directly from FieldEvent.TransitionTo + INotificationService - but they are NOT
/// part of the required E2E flow and are not the focus of testing effort.
/// </summary>
[ApiController]
[Authorize]
[Route("api/events")]
public class EventsController(AppDbContext db, INotificationService notifications) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Dispatcher))]
    public async Task<ActionResult<List<EventSummary>>> GetAll() =>
        await db.Events.OrderByDescending(e => e.Id).Select(ToSummary).ToListAsync();

    [HttpGet("mine")]
    [Authorize(Roles = nameof(UserRole.Technician))]
    public async Task<ActionResult<List<EventSummary>>> GetMine() =>
        await db.Events
            .Where(e => e.AssignedTechnicianId == CurrentUserId)
            .OrderByDescending(e => e.Id)
            .Select(ToSummary)
            .ToListAsync();

    [HttpGet("available")]
    [Authorize(Roles = nameof(UserRole.Technician))]
    public async Task<ActionResult<List<EventSummary>>> GetAvailable() =>
        await db.Events
            .Where(e => e.AssignedTechnicianId == null && e.Status == EventStatus.New)
            .OrderByDescending(e => e.Id)
            .Select(ToSummary)
            .ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventSummary>> GetById(int id)
    {
        var summary = await db.Events.Where(e => e.Id == id).Select(ToSummary).FirstOrDefaultAsync();
        return summary is null ? NotFound() : summary;
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<List<EventStatusHistory>>> GetHistory(int id) =>
        await db.EventStatusHistories
            .Where(h => h.FieldEventId == id)
            .OrderBy(h => h.Id)
            .ToListAsync();

    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = nameof(UserRole.Dispatcher))]
    public async Task<IActionResult> Assign(int id, AssignRequest request)
    {
        var evt = await db.Events.FindAsync(id);
        if (evt is null) return NotFound();

        evt.TransitionTo(EventStatus.Assigned, CurrentUserId);
        evt.AssignedTechnicianId = request.TechnicianId;
        await db.SaveChangesAsync();

        var summary = await BuildSummaryAsync(evt);
        await notifications.NotifyTechnicianAsync(request.TechnicianId, summary);
        await notifications.NotifyDispatchersNewEventAsync(summary);
        return NoContent();
    }

    [HttpPost("{id:int}/transfer")]
    [Authorize(Roles = nameof(UserRole.Dispatcher))]
    public async Task<IActionResult> Transfer(int id, AssignRequest request)
    {
        var evt = await db.Events.FindAsync(id);
        if (evt is null) return NotFound();

        var previousTechnicianId = evt.AssignedTechnicianId;
        evt.AssignedTechnicianId = request.TechnicianId;
        await db.SaveChangesAsync();

        var summary = await BuildSummaryAsync(evt);
        if (previousTechnicianId is not null)
        {
            await notifications.NotifyTechnicianAsync(previousTechnicianId.Value, summary);
        }
        await notifications.NotifyTechnicianAsync(request.TechnicianId, summary);
        await notifications.NotifyDispatchersNewEventAsync(summary);
        return NoContent();
    }

    [HttpPost("{id:int}/priority")]
    [Authorize(Roles = nameof(UserRole.Dispatcher))]
    public async Task<IActionResult> ChangePriority(int id, PriorityChangeRequest request)
    {
        var evt = await db.Events.FindAsync(id);
        if (evt is null) return NotFound();

        evt.Priority = request.NewPriority;
        await db.SaveChangesAsync();

        var summary = await BuildSummaryAsync(evt);
        await notifications.NotifyDispatchersNewEventAsync(summary);
        if (evt.AssignedTechnicianId is not null)
        {
            await notifications.NotifyTechnicianAsync(evt.AssignedTechnicianId.Value, summary);
        }
        return NoContent();
    }

    [HttpPost("{id:int}/claim")]
    [Authorize(Roles = nameof(UserRole.Technician))]
    public async Task<IActionResult> Claim(int id)
    {
        var evt = await db.Events.FindAsync(id);
        if (evt is null) return NotFound();
        if (evt.AssignedTechnicianId is not null) return Conflict("Event is already assigned.");

        evt.TransitionTo(EventStatus.Assigned, CurrentUserId);
        evt.AssignedTechnicianId = CurrentUserId;
        await db.SaveChangesAsync();

        await notifications.NotifyDispatchersNewEventAsync(await BuildSummaryAsync(evt));
        return NoContent();
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, StatusChangeRequest request)
    {
        var evt = await db.Events.FindAsync(id);
        if (evt is null) return NotFound();
        if (User.IsInRole(nameof(UserRole.Technician)) && evt.AssignedTechnicianId != CurrentUserId) return Forbid();

        evt.TransitionTo(request.NewStatus, CurrentUserId);
        await db.SaveChangesAsync();

        var summary = await BuildSummaryAsync(evt);
        await notifications.NotifyDispatchersNewEventAsync(summary);
        if (evt.AssignedTechnicianId is not null)
        {
            await notifications.NotifyTechnicianAsync(evt.AssignedTechnicianId.Value, summary);
        }
        return NoContent();
    }

    [HttpPost("{id:int}/comments")]
    [Authorize(Roles = nameof(UserRole.Technician))]
    public async Task<IActionResult> AddComment(int id, CommentRequest request)
    {
        var evt = await db.Events.FindAsync(id);
        if (evt is null) return NotFound();

        db.EventComments.Add(new EventComment { FieldEventId = id, UserId = CurrentUserId, Text = request.Text });
        await db.SaveChangesAsync();

        await notifications.NotifyDispatchersNewEventAsync(await BuildSummaryAsync(evt));
        return NoContent();
    }

    private static readonly System.Linq.Expressions.Expression<Func<FieldEvent, EventSummary>> ToSummary = e => new EventSummary
    {
        Id = e.Id,
        Title = e.Title,
        Description = e.Description,
        Location = e.Location,
        Source = e.Source,
        Priority = e.Priority,
        Status = e.Status,
        AssignedTechnicianId = e.AssignedTechnicianId,
        CreatedAtUtc = e.CreatedAtUtc,
        LatestComment = e.Comments.OrderByDescending(c => c.Id).Select(c => c.Text).FirstOrDefault()
    };

    /// <summary>
    /// Builds a summary for a just-mutated entity, including its latest comment - queried fresh
    /// since e.Comments isn't loaded on an entity fetched via FindAsync.
    /// </summary>
    private async Task<EventSummary> BuildSummaryAsync(FieldEvent e)
    {
        var latestComment = await db.EventComments
            .Where(c => c.FieldEventId == e.Id)
            .OrderByDescending(c => c.Id)
            .Select(c => c.Text)
            .FirstOrDefaultAsync();

        return new EventSummary
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            Location = e.Location,
            Source = e.Source,
            Priority = e.Priority,
            Status = e.Status,
            AssignedTechnicianId = e.AssignedTechnicianId,
            CreatedAtUtc = e.CreatedAtUtc,
            LatestComment = latestComment
        };
    }
}
