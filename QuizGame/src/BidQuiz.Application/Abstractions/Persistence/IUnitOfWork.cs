namespace BidQuiz.Application.Abstractions.Persistence;

/// <summary>
/// One short piece of work against the database. Entities read through the repositories are
/// tracked, and <see cref="SaveChangesAsync"/> writes every change to them in one go.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    ITeamRepository Teams { get; }

    IQuestionRepository Questions { get; }

    IBidRepository Bids { get; }

    IGameStateRepository Games { get; }

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
