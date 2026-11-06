using BidQuiz.Application.Abstractions.Persistence;

namespace BidQuiz.Application.Bids;

/// <summary>Reads the bids on a question, highest first and earliest first on a tie.</summary>
public static class BidList
{
    public static async Task<IReadOnlyList<BidResponse>> ReadAsync(
        IUnitOfWork work,
        int? questionId,
        CancellationToken cancellationToken)
    {
        var bids = await work.Bids.ListForQuestionAsync(questionId, cancellationToken);
        var teams = (await work.Teams.ListAsync(cancellationToken)).ToDictionary(t => t.Id);
        return bids
            .OrderByDescending(b => b.Amount)
            .ThenBy(b => b.SubmittedUtc)
            .Select(b => BidResponse.From(b, teams[b.TeamId]))
            .ToList();
    }
}
