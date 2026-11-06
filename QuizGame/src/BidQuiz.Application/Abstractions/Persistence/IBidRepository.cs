using BidQuiz.Domain.Bids;

namespace BidQuiz.Application.Abstractions.Persistence;

public interface IBidRepository
{
    /// <summary>Finds a bid to change. Null if there is no such bid.</summary>
    Task<Bid?> FindAsync(int bidId, CancellationToken cancellationToken);

    /// <summary>Finds a team's bid on a question, to change. Null if it hasn't bid.</summary>
    Task<Bid?> FindForTeamAsync(int teamId, int questionId, CancellationToken cancellationToken);

    /// <summary>The bids on a question. Read-only.</summary>
    Task<IReadOnlyList<Bid>> ListForQuestionAsync(int? questionId, CancellationToken cancellationToken);

    Task<int> CountForQuestionAsync(int? questionId, CancellationToken cancellationToken);

    /// <summary>The highest amount bid on a question that has at least one bid.</summary>
    Task<int> HighestAmountAsync(int questionId, CancellationToken cancellationToken);

    void Add(Bid bid);

    /// <summary>Deletes every bid. Runs at once, not on save.</summary>
    Task DeleteAllAsync(CancellationToken cancellationToken);
}
