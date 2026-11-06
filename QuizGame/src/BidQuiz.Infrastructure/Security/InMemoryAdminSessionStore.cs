using System.Collections.Concurrent;
using BidQuiz.Application.Abstractions.Security;

namespace BidQuiz.Infrastructure.Security;

/// <summary>Admin tokens are kept in memory, so a restart asks the host to log in again.</summary>
public sealed class InMemoryAdminSessionStore : IAdminSessionStore
{
    private readonly ConcurrentDictionary<string, byte> _tokens = new();

    public void Add(string sessionToken) => _tokens[sessionToken] = 0;

    public bool Contains(string sessionToken) => _tokens.ContainsKey(sessionToken);
}
