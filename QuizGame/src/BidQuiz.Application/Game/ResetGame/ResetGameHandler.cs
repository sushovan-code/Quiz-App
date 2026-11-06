using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Realtime;
using BidQuiz.Application.Common;
using BidQuiz.Application.Questions;
using BidQuiz.Application.Scores;
using BidQuiz.Domain.Questions;
using BidQuiz.Domain.Scoring;

namespace BidQuiz.Application.Game.ResetGame;

/// <summary>Clears the scores and deals a fresh set of questions for the next game.</summary>
public sealed class ResetGameHandler(
    IUnitOfWorkFactory unitOfWork,
    GameGate gate,
    QuestionDeadline deadline,
    Scoreboard scoreboard,
    IScoringPolicy scoring,
    IQuestionPicker picker,
    IGameNotifier notifier)
{
    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        using var lease = await gate.EnterAsync(cancellationToken);
        await using var work = await unitOfWork.BeginAsync(cancellationToken);
        await work.Bids.DeleteAllAsync(cancellationToken);
        await work.Teams.ResetBalancesAsync(scoring.Opening, cancellationToken);

        var state = await work.Games.GetAsync(cancellationToken);
        state.Reset();
        QuestionDeck.Deal(await work.Questions.ListAsync(cancellationToken), state.QuestionsPerGame, picker);
        await work.SaveChangesAsync(cancellationToken);
        deadline.Clear();

        await notifier.GameResetAsync(AfterCommit.Token);
        await notifier.QuestionBoardChangedAsync(await QuestionBoard.ReadAsync(work, AfterCommit.Token), AfterCommit.Token);
        await notifier.ScoresUpdatedAsync(await scoreboard.ReadAsync(work, AfterCommit.Token), AfterCommit.Token);
    }
}
