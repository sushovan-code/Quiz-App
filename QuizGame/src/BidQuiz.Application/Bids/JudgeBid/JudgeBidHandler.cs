using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Realtime;
using BidQuiz.Application.Common;
using BidQuiz.Application.Scores;
using BidQuiz.Domain.Bids;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Scoring;
using BidQuiz.Domain.Teams;

namespace BidQuiz.Application.Bids.JudgeBid;

/// <summary>
/// Applies the host's thumbs up or down. Changing a verdict first undoes what the last one
/// really changed, so nothing is double-counted.
/// </summary>
public sealed class JudgeBidHandler(
    IUnitOfWorkFactory unitOfWork,
    GameGate gate,
    Scoreboard scoreboard,
    IScoringPolicy scoring,
    IGameNotifier notifier)
{
    public async Task<Result<BidResponse>> HandleAsync(JudgeBid command, CancellationToken cancellationToken)
    {
        using var lease = await gate.EnterAsync(cancellationToken);
        await using var work = await unitOfWork.BeginAsync(cancellationToken);
        var state = await work.Games.GetAsync(cancellationToken);
        var bid = await work.Bids.FindAsync(command.BidId, cancellationToken);
        if (bid is null)
        {
            return BidErrors.UnknownBid;
        }

        if (!state.IsOpenForJudging(bid))
        {
            return BidErrors.NotOpenForJudging;
        }

        var highest = await work.Bids.HighestAmountAsync(bid.QuestionId, cancellationToken);
        var team = await work.Teams.FindAsync(bid.TeamId, cancellationToken);
        if (team is null)
        {
            return TeamErrors.UnknownTeam;
        }

        var before = bid.BalancesBeforeVerdict(team.Balances);
        var after = scoring.Settle(before, command.Verdict, bid.Amount, bid.Amount == highest);
        bid.RecordVerdict(command.Verdict, before, after);
        team.SetBalances(after);
        await work.SaveChangesAsync(cancellationToken);

        await notifier.ScoresUpdatedAsync(await scoreboard.ReadAsync(work, AfterCommit.Token), AfterCommit.Token);
        return BidResponse.From(bid, team);
    }
}
