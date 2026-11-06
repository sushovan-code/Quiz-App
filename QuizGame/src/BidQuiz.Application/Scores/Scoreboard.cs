using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Presence;
using BidQuiz.Domain.Scoring;

namespace BidQuiz.Application.Scores;

/// <summary>Reads the leaderboard of the teams playing this game.</summary>
public sealed class Scoreboard(ITeamPresence presence, IScoringPolicy scoring)
{
    public async Task<IReadOnlyList<TeamScoreResponse>> ReadAsync(IUnitOfWork work, CancellationToken cancellationToken)
    {
        var teams = await work.Teams.ListActiveAsync(cancellationToken);
        return teams
            .OrderByDescending(t => t.Wallet)
            .ThenByDescending(t => t.BidBalance)
            .ThenBy(t => t.Name, StringComparer.CurrentCulture)
            .Select(t => new TeamScoreResponse(
                t.Id,
                t.Name,
                t.Color,
                t.BidBalance,
                t.Wallet,
                scoring.CanBid(t.BidBalance),
                presence.IsOnline(t.Id)))
            .ToList();
    }

    /// <summary>Teams still able to bid, which is who "X of Y answered" counts.</summary>
    public async Task<int> CountBiddersAsync(IUnitOfWork work, CancellationToken cancellationToken)
    {
        var teams = await work.Teams.ListActiveAsync(cancellationToken);
        return teams.Count(t => scoring.CanBid(t.BidBalance));
    }
}
