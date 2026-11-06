using System.Collections.Concurrent;

namespace BidQuiz.Server.Game;

/// <summary>Tracks which connections belong to which team. Several phones may share a team.</summary>
public sealed class PresenceTracker : IPresence
{
    private readonly ConcurrentDictionary<string, int> _connections = new();

    /// <summary>Returns true if the team has just come online.</summary>
    public bool Add(string connectionId, int teamId)
    {
        var wasOnline = IsOnline(teamId);
        _connections[connectionId] = teamId;
        return !wasOnline;
    }

    /// <summary>Returns the team that has just gone offline, if any.</summary>
    public int? Remove(string connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var teamId))
            return null;
        return IsOnline(teamId) ? null : teamId;
    }

    public bool IsOnline(int teamId) => _connections.Values.Contains(teamId);
}
