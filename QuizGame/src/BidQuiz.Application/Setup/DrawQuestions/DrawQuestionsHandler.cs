using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Questions;

namespace BidQuiz.Application.Setup.DrawQuestions;

/// <summary>Deals a new random set of questions from the bank.</summary>
public sealed class DrawQuestionsHandler(SetupChanges changes, IQuestionPicker picker)
{
    public Task<Result<SetupResponse>> HandleAsync(CancellationToken cancellationToken) =>
        changes.ApplyAsync(DrawAsync, cancellationToken);

    private async Task<Result> DrawAsync(IUnitOfWork work, CancellationToken cancellationToken)
    {
        var state = await work.Games.GetAsync(cancellationToken);
        QuestionDeck.Deal(await work.Questions.ListAsync(cancellationToken), state.QuestionsPerGame, picker);
        return Result.Success();
    }
}
