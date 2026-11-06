using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Realtime;
using BidQuiz.Application.Common;
using BidQuiz.Application.Scores;
using BidQuiz.Domain.Bids;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Scoring;
using BidQuiz.Domain.Teams;

namespace BidQuiz.Application.Bids.SubmitBid;

/// <summary>Records a team's answer and bid. A team may resubmit until time is up; the last one wins.</summary>
public sealed class SubmitBidHandler(
    IUnitOfWorkFactory unitOfWork,
    GameGate gate,
    Scoreboard scoreboard,
    IScoringPolicy scoring,
    IGameNotifier notifier,
    TimeProvider time)
{
    public async Task<Result<MyBidResponse>> HandleAsync(SubmitBid command, CancellationToken cancellationToken)
    {
        using var lease = await gate.EnterAsync(cancellationToken);
        await using var work = await unitOfWork.BeginAsync(cancellationToken);
        var state = await work.Games.GetAsync(cancellationToken);
        var now = time.GetUtcNow().UtcDateTime;

        var open = state.AcceptsBidsAt(now);
        if (open.IsFailure)
        {
            return open.Error!;
        }

        var answer = BidAnswer.Normalize(command.Answer);
        if (answer.IsFailure)
        {
            return answer.Error!;
        }

        var team = await work.Teams.FindAsync(command.TeamId, cancellationToken);
        if (team is null)
        {
            return TeamErrors.UnknownTeam;
        }

        var valid = scoring.ValidateBid(command.Amount, team.BidBalance);
        if (valid.IsFailure)
        {
            return valid.Error!;
        }

        var questionId = state.CurrentQuestionId!.Value;
        var bid = await work.Bids.FindForTeamAsync(team.Id, questionId, cancellationToken);
        var isFirstSubmission = bid is null;
        if (bid is null)
        {
            bid = Bid.Place(team.Id, questionId, answer.Value, command.Amount, now);
            work.Bids.Add(bid);
        }
        else
        {
            bid.Revise(answer.Value, command.Amount, now);
        }

        await work.SaveChangesAsync(cancellationToken);
        if (isFirstSubmission)
        {
            await notifier.AnswerCountChangedAsync(
                await work.Bids.CountForQuestionAsync(questionId, AfterCommit.Token),
                await scoreboard.CountBiddersAsync(work, AfterCommit.Token),
                AfterCommit.Token);
        }

        return new MyBidResponse(bid.Answer, bid.Amount);
    }
}
