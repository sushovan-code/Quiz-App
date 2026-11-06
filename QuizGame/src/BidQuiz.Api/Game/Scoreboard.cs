using BidQuiz.Api.Data;
using BidQuiz.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BidQuiz.Api.Game;

public sealed class Scoreboard(IPresence presence, IScoringPolicy scoring)
{
    public async Task<IReadOnlyList<TeamScoreDto>> ReadAsync(GameDbContext db)
    {
        var teams = await db.Teams.AsNoTracking().Where(t => t.Active).ToListAsync();
        return teams
            .OrderByDescending(t => t.Wallet)
            .ThenByDescending(t => t.BidBalance)
            .ThenBy(t => t.Name)
            .Select(t => new TeamScoreDto(
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
    public async Task<int> CountActiveAsync(GameDbContext db)
    {
        var balances = await db.Teams.AsNoTracking().Where(t => t.Active).Select(t => t.BidBalance).ToListAsync();
        return balances.Count(scoring.CanBid);
    }
}
