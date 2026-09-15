using FieldEvents.Server.Auth;
using FieldEvents.Server.Data;
using FieldEvents.Server.Data.Entities;
using FieldEvents.Server.Services;
using FieldEvents.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FieldEvents.Server.Hubs;

/// <summary>Receives events pushed by the Agent. This is steps 2-4 of the required E2E flow.</summary>
[Authorize(AuthenticationSchemes = AgentApiKeyOptions.SchemeName)]
public class EventsHub(AppDbContext db, INotificationService notifications, ILogger<EventsHub> logger) : Hub
{
    public async Task<EventAckResponse> ReportEvent(AgentEventMessage message)
    {
        var existing = await db.Events.FirstOrDefaultAsync(e => e.OutboxId == message.OutboxId);
        if (existing is not null)
        {
            // Idempotent retry: the Agent already sent this once but never saw the ack.
            logger.LogInformation("Duplicate OutboxId {OutboxId} ignored, returning existing event {EventId}.", message.OutboxId, existing.Id);
            return new EventAckResponse { OutboxId = message.OutboxId, Success = true, ServerEventId = existing.Id };
        }

        var entity = new FieldEvent
        {
            Title = message.Title,
            Description = message.Description,
            Location = message.Location,
            Source = message.SourceId,
            Priority = message.Priority,
            ExternalRef = message.ExternalRef,
            OutboxId = message.OutboxId,
            CreatedAtUtc = message.OccurredAtUtc
        };

        db.Events.Add(entity);
        await db.SaveChangesAsync();

        var summary = new EventSummary
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Location = entity.Location,
            Source = entity.Source,
            Priority = entity.Priority,
            Status = entity.Status,
            CreatedAtUtc = entity.CreatedAtUtc
        };

        await notifications.NotifyDispatchersNewEventAsync(summary);

        return new EventAckResponse { OutboxId = message.OutboxId, Success = true, ServerEventId = entity.Id };
    }
}
