using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Realtime;
using BidQuiz.Application.Bids;
using BidQuiz.Application.Common;
using BidQuiz.Domain.Common;

namespace BidQuiz.Application.Game.RevealBids;

/// <summary>Shows the bids to the host. Revealing early closes bidding.</summary>
public sealed class RevealBidsHandler(
    IUnitOfWorkFactory unitOfWork,
    GameGate gate,
    QuestionDeadline deadline,
    IGameNotifier notifier)
{
    public async Task<Result> HandleAsync(CancellationToken cancellationToken)
    {
        using var lease = await gate.EnterAsync(cancellationToken);
        await using var work = await unitOfWork.BeginAsync(cancellationToken);
        var state = await work.Games.GetAsync(cancellationToken);
        var revealed = state.RevealBids();
        if (revealed.IsFailure)
        {
            return revealed;
        }

        await work.SaveChangesAsync(cancellationToken);
        deadline.Clear();

        if (revealed.Value)
        {
            await notifier.TimeUpAsync(AfterCommit.Token);
        }

        var bids = await BidList.ReadAsync(work, state.CurrentQuestionId, AfterCommit.Token);
        await notifier.BidsRevealedAsync(bids, AfterCommit.Token);
        return Result.Success();
    }
}
