using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Infrastructure.Persistence.Repositories;

namespace BidQuiz.Infrastructure.Persistence;

/// <summary>One DbContext per unit of work; EF's change tracking saves what the use case changed.</summary>
public sealed class EfUnitOfWork(BidQuizDbContext context) : IUnitOfWork
{
    public ITeamRepository Teams { get; } = new EfTeamRepository(context);

    public IQuestionRepository Questions { get; } = new EfQuestionRepository(context);

    public IBidRepository Bids { get; } = new EfBidRepository(context);

    public IGameStateRepository Games { get; } = new EfGameStateRepository(context);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
