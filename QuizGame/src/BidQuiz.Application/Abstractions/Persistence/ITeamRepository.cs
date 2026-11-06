using BidQuiz.Domain.Scoring;
using BidQuiz.Domain.Teams;

namespace BidQuiz.Application.Abstractions.Persistence;

public interface ITeamRepository
{
    /// <summary>Finds a team to change. Null if there is no such team.</summary>
    Task<Team?> FindAsync(int teamId, CancellationToken cancellationToken);

    /// <summary>Finds the playing team that owns a session token. Read-only.</summary>
    Task<Team?> FindActiveBySessionTokenAsync(string sessionToken, CancellationToken cancellationToken);

    /// <summary>Every team on the roster, in roster order (by id), ready to change.</summary>
    Task<IReadOnlyList<Team>> ListAsync(CancellationToken cancellationToken);

    /// <summary>The teams playing this game. Read-only.</summary>
    Task<IReadOnlyList<Team>> ListActiveAsync(CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);

    void Add(Team team);

    void Remove(Team team);

    /// <summary>Sets every team's balances. Runs at once, not on save.</summary>
    Task ResetBalancesAsync(Balances opening, CancellationToken cancellationToken);
}
