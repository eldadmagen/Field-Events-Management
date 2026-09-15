using FieldEvents.Shared;
using FieldEvents.Shared.Contracts;
using Microsoft.Data.Sqlite;

namespace FieldEvents.Agent.Outbox;

/// <summary>
/// Local, disk-persisted queue the Agent writes to BEFORE attempting to forward to the Server.
/// This is what stops an event from being lost if the Server is unreachable, or if the Agent
/// process itself restarts mid-outage: rows only leave the table once the Server has ack'd them.
/// </summary>
public class OutboxStore(IConfiguration config)
{
    private readonly string _connectionString = $"Data Source={config["Outbox:DbPath"] ?? "agent-outbox.db"}";

    public async Task InitializeAsync()
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS OutboxEvents (
                OutboxId TEXT PRIMARY KEY,
                SourceId TEXT NOT NULL,
                Title TEXT NOT NULL,
                Description TEXT NOT NULL,
                Location TEXT NULL,
                Priority INTEGER NOT NULL,
                ExternalRef TEXT NULL,
                OccurredAtUtc TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EnqueueAsync(AgentEventMessage message)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO OutboxEvents (OutboxId, SourceId, Title, Description, Location, Priority, ExternalRef, OccurredAtUtc, CreatedAtUtc)
            VALUES ($outboxId, $sourceId, $title, $description, $location, $priority, $externalRef, $occurredAt, $createdAt);
            """;
        cmd.Parameters.AddWithValue("$outboxId", message.OutboxId.ToString());
        cmd.Parameters.AddWithValue("$sourceId", message.SourceId);
        cmd.Parameters.AddWithValue("$title", message.Title);
        cmd.Parameters.AddWithValue("$description", message.Description);
        cmd.Parameters.AddWithValue("$location", (object?)message.Location ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$priority", (int)message.Priority);
        cmd.Parameters.AddWithValue("$externalRef", (object?)message.ExternalRef ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$occurredAt", message.OccurredAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$createdAt", DateTimeOffset.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<AgentEventMessage>> GetPendingAsync()
    {
        var results = new List<AgentEventMessage>();
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT OutboxId, SourceId, Title, Description, Location, Priority, ExternalRef, OccurredAtUtc FROM OutboxEvents ORDER BY CreatedAtUtc;";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new AgentEventMessage
            {
                OutboxId = Guid.Parse(reader.GetString(0)),
                SourceId = reader.GetString(1),
                Title = reader.GetString(2),
                Description = reader.GetString(3),
                Location = reader.IsDBNull(4) ? null : reader.GetString(4),
                Priority = (EventPriority)reader.GetInt32(5),
                ExternalRef = reader.IsDBNull(6) ? null : reader.GetString(6),
                OccurredAtUtc = DateTimeOffset.Parse(reader.GetString(7))
            });
        }
        return results;
    }

    public async Task MarkSentAsync(Guid outboxId)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM OutboxEvents WHERE OutboxId = $outboxId;";
        cmd.Parameters.AddWithValue("$outboxId", outboxId.ToString());
        await cmd.ExecuteNonQueryAsync();
    }
}
