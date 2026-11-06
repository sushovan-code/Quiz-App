namespace BidQuiz.Application.Abstractions.Persistence;

/// <summary>Opens a unit of work. Implemented by each database adapter (SQLite today, MongoDB later).</summary>
public interface IUnitOfWorkFactory
{
    Task<IUnitOfWork> BeginAsync(CancellationToken cancellationToken);
}
