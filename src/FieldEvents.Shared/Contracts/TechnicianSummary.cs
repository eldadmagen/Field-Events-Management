namespace FieldEvents.Shared.Contracts;

/// <summary>Dispatcher-facing technician row: identity plus live connection state, for the technician status dashboard.</summary>
public sealed class TechnicianSummary
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
}
