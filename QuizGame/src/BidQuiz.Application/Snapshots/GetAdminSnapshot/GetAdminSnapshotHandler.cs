using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Bids;
using BidQuiz.Application.Questions;
using BidQuiz.Application.Scores;

namespace BidQuiz.Application.Snapshots.GetAdminSnapshot;

public sealed class GetAdminSnapshotHandler(IUnitOfWorkFactory unitOfWork, Scoreboard scoreboard, TimeProvider time)
{
    public async Task<AdminSnapshotResponse> HandleAsync(CancellationToken cancellationToken)
    {
        await using var work = await unitOfWork.BeginAsync(cancellationToken);
        var state = await work.Games.GetAsync(cancellationToken);
        var question = state.CurrentQuestionId is { } questionId
            ? await work.Questions.FindAsync(questionId, cancellationToken)
            : null;
        var revealed = state.AreBidsRevealed;

        return new AdminSnapshotResponse(
            state.Status,
            question is null ? null : QuestionResponse.From(question),
            state.OpenDeadline,
            time.GetUtcNow().UtcDateTime,
            await work.Bids.CountForQuestionAsync(state.CurrentQuestionId, cancellationToken),
            await scoreboard.CountBiddersAsync(work, cancellationToken),
            await QuestionBoard.ReadAsync(work, cancellationToken),
            await scoreboard.ReadAsync(work, cancellationToken),
            revealed ? await BidList.ReadAsync(work, state.CurrentQuestionId, cancellationToken) : null,
            revealed && state.IsAnswerRevealed ? question?.CorrectAnswer : null);
    }
}
