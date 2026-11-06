using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Domain.Games;
using Microsoft.EntityFrameworkCore;

namespace BidQuiz.Infrastructure.Persistence.Repositories;

public sealed class EfGameStateRepository(BidQuizDbContext context) : IGameStateRepository
{
    public Task<GameState> GetAsync(CancellationToken cancellationToken) =>
        context.GameStates.SingleAsync(cancellationToken);

    public Task<GameState?> FindAsync(CancellationToken cancellationToken) =>
        context.GameStates.SingleOrDefaultAsync(cancellationToken);

    public void Add(GameState state) => context.GameStates.Add(state);
}
