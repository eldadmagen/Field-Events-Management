using System.Collections.Concurrent;

namespace FieldEvents.Server.Hubs;

/// <summary>
/// Tracks which users currently have a live ClientsHub connection. This is what lets the
/// notification layer decide "send over SignalR" vs "fall back to push" - see Services/NotificationService.cs.
/// In-memory and per-process by design (single Server instance for this exercise's scope);
/// a multi-instance deployment would move this to a shared backplane (e.g. Redis).
/// </summary>
public class ConnectionManager
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();
    private readonly object _lock = new();

    public void AddConnection(string userId, string connectionId)
    {
        lock (_lock)
        {
            if (!_userConnections.TryGetValue(userId, out var set))
            {
                set = [];
                _userConnections[userId] = set;
            }
            set.Add(connectionId);
        }
    }

    public void RemoveConnection(string userId, string connectionId)
    {
        lock (_lock)
        {
            if (_userConnections.TryGetValue(userId, out var set))
            {
                set.Remove(connectionId);
                if (set.Count == 0)
                {
                    _userConnections.TryRemove(userId, out _);
                }
            }
        }
    }

    public bool IsOnline(string userId) => _userConnections.ContainsKey(userId);
}
