using FieldEvents.Shared.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace FieldEvents.Agent.Outbox;

/// <summary>
/// Owns the persistent SignalR connection to the Server and continuously drains the outbox into it.
/// Two layers of resilience: HubConnection.WithAutomaticReconnect() handles short blips fast, and this
/// loop's own reconnect-if-disconnected check handles a prolonged outage (keeps retrying forever at a
/// fixed poll interval instead of giving up once the built-in retry policy is exhausted).
/// </summary>
public class OutboxForwarder(OutboxStore outbox, IConfiguration config, ILogger<OutboxForwarder> logger) : BackgroundService
{
    private HubConnection? _connection;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await outbox.InitializeAsync();

        var serverUrl = config["Server:BaseUrl"]!.TrimEnd('/') + HubRoutes.EventsHubPath;
        var agentKey = config["Agent:ApiKey"]!;

        _connection = new HubConnectionBuilder()
            .WithUrl(serverUrl, options => options.Headers["X-Agent-Key"] = agentKey)
            .WithAutomaticReconnect()
            .Build();

        _connection.Closed += ex =>
        {
            logger.LogWarning(ex, "Hub connection closed, will retry connecting.");
            return Task.CompletedTask;
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_connection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _connection.StartAsync(stoppingToken);
                    logger.LogInformation("Connected to server hub at {Url}.", serverUrl);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Server unreachable, will keep retrying. Events keep accumulating in the local outbox.");
                }
            }

            if (_connection.State == HubConnectionState.Connected)
            {
                await DrainOutboxAsync(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task DrainOutboxAsync(CancellationToken ct)
    {
        var pending = await outbox.GetPendingAsync();

        foreach (var message in pending)
        {
            if (_connection!.State != HubConnectionState.Connected) return;

            try
            {
                var ack = await _connection.InvokeAsync<EventAckResponse>(HubRoutes.ReportEventMethod, message, ct);
                if (ack.Success)
                {
                    await outbox.MarkSentAsync(message.OutboxId);
                    logger.LogInformation("Forwarded event {OutboxId} -> server event {ServerEventId}.", message.OutboxId, ack.ServerEventId);
                }
                else
                {
                    logger.LogWarning("Server rejected event {OutboxId}: {Error}", message.OutboxId, ack.Error);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to forward event {OutboxId}, leaving it in the outbox for retry.", message.OutboxId);
                return; // connection likely just dropped - stop this pass, next loop iteration reconnects
            }
        }
    }
}
