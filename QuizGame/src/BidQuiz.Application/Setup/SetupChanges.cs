using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Realtime;
using BidQuiz.Application.Common;
using BidQuiz.Application.Questions;
using BidQuiz.Application.Scores;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Games;
using BidQuiz.Domain.Questions;
using BidQuiz.Domain.Teams;

namespace BidQuiz.Application.Setup;

/// <summary>
/// Runs a setup change and keeps the game consistent afterwards: the board is refilled and the
/// roster reapplied, then everyone is told. Setup can only change before the first question,
/// because mid-game edits would orphan bids and scores.
/// </summary>
public sealed class SetupChanges(
    IUnitOfWorkFactory unitOfWork,
    GameGate gate,
    Scoreboard scoreboard,
    IQuestionPicker picker,
    IGameNotifier notifier)
{
    public async Task<Result<SetupResponse>> ApplyAsync(SetupChange change, CancellationToken cancellationToken)
    {
        using var lease = await gate.EnterAsync(cancellationToken);
        await using var work = await unitOfWork.BeginAsync(cancellationToken);
        if (await work.Questions.AnyAskedAsync(cancellationToken))
        {
            return GameErrors.SetupLocked;
        }

        var changed = await change(work, cancellationToken);
        if (changed.IsFailure)
        {
            return changed.Error!;
        }

        await work.SaveChangesAsync(cancellationToken);
        await RefillBoardAsync(work, cancellationToken);
        TeamRoster.Apply(await work.Games.GetAsync(cancellationToken), await work.Teams.ListAsync(cancellationToken));
        await work.SaveChangesAsync(cancellationToken);

        await notifier.RosterChangedAsync(AfterCommit.Token);
        await notifier.QuestionBoardChangedAsync(await QuestionBoard.ReadAsync(work, AfterCommit.Token), AfterCommit.Token);
        await notifier.ScoresUpdatedAsync(await scoreboard.ReadAsync(work, AfterCommit.Token), AfterCommit.Token);
        return await SetupReader.ReadAsync(work, AfterCommit.Token);
    }

    /// <summary>Deals again when a change leaves the board short or over, for example a deleted question.</summary>
    private async Task RefillBoardAsync(IUnitOfWork work, CancellationToken cancellationToken)
    {
        var state = await work.Games.GetAsync(cancellationToken);
        var expected = Math.Min(state.QuestionsPerGame, await work.Questions.CountAsync(cancellationToken));
        if (await work.Questions.CountInGameAsync(cancellationToken) == expected)
        {
            return;
        }

        QuestionDeck.Deal(await work.Questions.ListAsync(cancellationToken), state.QuestionsPerGame, picker);
        await work.SaveChangesAsync(cancellationToken);
    }
}
