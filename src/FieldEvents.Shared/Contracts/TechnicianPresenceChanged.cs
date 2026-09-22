namespace FieldEvents.Shared.Contracts;

/// <summary>Payload for HubRoutes.TechnicianPresenceChangedMethod - pushed to the Dispatchers group whenever a technician connects/disconnects from ClientsHub.</summary>
public sealed class TechnicianPresenceChanged
{
    public int TechnicianId { get; set; }
    public bool IsOnline { get; set; }
}
