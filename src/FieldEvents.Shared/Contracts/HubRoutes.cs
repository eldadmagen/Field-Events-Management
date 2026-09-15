namespace FieldEvents.Shared.Contracts;

/// <summary>Hub paths and method names shared between Agent, Server and Client so they never drift apart.</summary>
public static class HubRoutes
{
    public const string EventsHubPath = "/hubs/events";     // Agent -> Server
    public const string ClientsHubPath = "/hubs/clients";   // Server -> Dispatcher/Technician UIs

    public const string ReportEventMethod = "ReportEvent";       // Agent calls this on EventsHub
    public const string NewEventReceivedMethod = "NewEventReceived"; // Server pushes this on ClientsHub
}
