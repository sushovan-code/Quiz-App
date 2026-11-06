using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Domain.Scoring;
using BidQuiz.Domain.Teams;
using Microsoft.EntityFrameworkCore;

namespace BidQuiz.Infrastructure.Persistence.Repositories;

public sealed class EfTeamRepository(BidQuizDbContext context) : ITeamRepository
{
    public Task<Team?> FindAsync(int teamId, CancellationToken cancellationToken) =>
        context.Teams.FindAsync([teamId], cancellationToken).AsTask();

    public Task<Team?> FindActiveBySessionTokenAsync(string sessionToken, CancellationToken cancellationToken) =>
        context.Teams.AsNoTracking()
            .SingleOrDefaultAsync(t => t.SessionToken == sessionToken && t.IsActive, cancellationToken);

    public async Task<IReadOnlyList<Team>> ListAsync(CancellationToken cancellationToken) =>
        await context.Teams.OrderBy(t => t.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Team>> ListActiveAsync(CancellationToken cancellationToken) =>
        await context.Teams.AsNoTracking().Where(t => t.IsActive).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken) => context.Teams.CountAsync(cancellationToken);

    public void Add(Team team) => context.Teams.Add(team);

    public void Remove(Team team) => context.Teams.Remove(team);

    public Task ResetBalancesAsync(Balances opening, CancellationToken cancellationToken) =>
        context.Teams.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(t => t.BidBalance, opening.BidBalance)
                .SetProperty(t => t.Wallet, opening.Wallet),
            cancellationToken);
}
