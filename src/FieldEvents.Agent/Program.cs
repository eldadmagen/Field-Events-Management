using FieldEvents.Agent.Outbox;
using FieldEvents.Agent.Sources;
using FieldEvents.Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OutboxStore>();
builder.Services.AddSingleton<SourceRegistry>();
builder.Services.AddHostedService<OutboxForwarder>();

var app = builder.Build();

// Generic ingestion endpoint any external source (sensor, external system, manual report tool)
// posts to. Extending to a new source = register its id + API key in config, nothing else changes.
app.MapPost("/ingest/{sourceId}", async (string sourceId, IncomingEventRequest request, HttpRequest http, SourceRegistry sources, OutboxStore outbox) =>
{
    var apiKey = http.Headers["X-Api-Key"].FirstOrDefault();
    if (apiKey is null || !sources.Validate(sourceId, apiKey))
    {
        return Results.Unauthorized();
    }

    var message = new AgentEventMessage
    {
        OutboxId = Guid.NewGuid(),
        SourceId = sourceId,
        Title = request.Title,
        Description = request.Description,
        Location = request.Location,
        Priority = request.Priority,
        ExternalRef = request.ExternalRef,
        OccurredAtUtc = DateTimeOffset.UtcNow
    };

    // Write-ahead to the local outbox and return immediately - the caller gets a fast ack even
    // if the Server is currently down; OutboxForwarder guarantees eventual delivery in the background.
    await outbox.EnqueueAsync(message);

    return Results.Accepted(value: new { outboxId = message.OutboxId });
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
