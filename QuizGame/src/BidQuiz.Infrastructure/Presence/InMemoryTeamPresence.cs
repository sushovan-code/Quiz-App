using System.Collections.Concurrent;
using BidQuiz.Application.Abstractions.Presence;

namespace BidQuiz.Infrastructure.Presence;

/// <summary>Tracks which connections belong to which team, in this process.</summary>
public sealed class InMemoryTeamPresence : ITeamPresence
{
    private readonly ConcurrentDictionary<string, int> _connections = new();

    public bool Add(string connectionId, int teamId)
    {
        var wasOnline = IsOnline(teamId);
        _connections[connectionId] = teamId;
        return !wasOnline;
    }

    public int? Remove(string connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var teamId))
        {
            return null;
        }

        return IsOnline(teamId) ? null : teamId;
    }

    public bool IsOnline(int teamId) => _connections.Values.Contains(teamId);
}
