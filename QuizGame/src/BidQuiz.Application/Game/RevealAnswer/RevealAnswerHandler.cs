using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Realtime;
using BidQuiz.Application.Common;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Questions;

namespace BidQuiz.Application.Game.RevealAnswer;

/// <summary>Shows the correct answer to the host, after the bids.</summary>
public sealed class RevealAnswerHandler(IUnitOfWorkFactory unitOfWork, GameGate gate, IGameNotifier notifier)
{
    public async Task<Result> HandleAsync(CancellationToken cancellationToken)
    {
        using var lease = await gate.EnterAsync(cancellationToken);
        await using var work = await unitOfWork.BeginAsync(cancellationToken);
        var state = await work.Games.GetAsync(cancellationToken);
        var revealed = state.RevealAnswer();
        if (revealed.IsFailure)
        {
            return revealed;
        }

        var question = state.CurrentQuestionId is { } questionId
            ? await work.Questions.FindAsync(questionId, cancellationToken)
            : null;
        if (question is null)
        {
            return QuestionErrors.UnknownQuestion;
        }

        await work.SaveChangesAsync(cancellationToken);
        await notifier.AnswerRevealedAsync(question.CorrectAnswer, AfterCommit.Token);
        return Result.Success();
    }
}
