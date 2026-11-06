namespace BidQuiz.Application.Abstractions.Security;

/// <summary>The host's session tokens. The in-memory adapter forgets them on restart, so the host logs in again.</summary>
public interface IAdminSessionStore
{
    void Add(string sessionToken);

    bool Contains(string sessionToken);

    /// <summary>Revokes a token. Unknown tokens are ignored.</summary>
    void Remove(string sessionToken);
}
