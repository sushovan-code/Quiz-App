using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Bids;
using BidQuiz.Application.Questions;
using BidQuiz.Application.Scores;

namespace BidQuiz.Application.Snapshots.GetTeamSnapshot;

/// <summary>A team only sees the question while it is open, and only its own bid.</summary>
public sealed class GetTeamSnapshotHandler(IUnitOfWorkFactory unitOfWork, Scoreboard scoreboard, TimeProvider time)
{
    public async Task<TeamSnapshotResponse> HandleAsync(GetTeamSnapshot query, CancellationToken cancellationToken)
    {
        await using var work = await unitOfWork.BeginAsync(cancellationToken);
        var state = await work.Games.GetAsync(cancellationToken);
        QuestionResponse? question = null;
        MyBidResponse? myBid = null;

        if (state.IsAccepting && state.CurrentQuestionId is { } questionId)
        {
            var open = await work.Questions.FindAsync(questionId, cancellationToken);
            question = open is null ? null : QuestionResponse.From(open);
            var bid = await work.Bids.FindForTeamAsync(query.TeamId, questionId, cancellationToken);
            myBid = bid is null ? null : new MyBidResponse(bid.Answer, bid.Amount);
        }

        return new TeamSnapshotResponse(
            query.TeamId,
            state.Status,
            question,
            state.OpenDeadline,
            time.GetUtcNow().UtcDateTime,
            myBid,
            await scoreboard.ReadAsync(work, cancellationToken));
    }
}
