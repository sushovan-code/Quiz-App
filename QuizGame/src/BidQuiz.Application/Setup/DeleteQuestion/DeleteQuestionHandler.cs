using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Games;
using BidQuiz.Domain.Questions;

namespace BidQuiz.Application.Setup.DeleteQuestion;

/// <summary>The bank can't shrink below the number of questions a game needs.</summary>
public sealed class DeleteQuestionHandler(SetupChanges changes)
{
    public Task<Result<SetupResponse>> HandleAsync(DeleteQuestion command, CancellationToken cancellationToken) =>
        changes.ApplyAsync((work, token) => DeleteAsync(work, command.QuestionId, token), cancellationToken);

    private static async Task<Result> DeleteAsync(IUnitOfWork work, int questionId, CancellationToken cancellationToken)
    {
        var question = await work.Questions.FindAsync(questionId, cancellationToken);
        if (question is null)
        {
            return QuestionErrors.UnknownQuestion;
        }

        var state = await work.Games.GetAsync(cancellationToken);
        if (await work.Questions.CountAsync(cancellationToken) <= state.QuestionsPerGame)
        {
            return GameErrors.BankTooSmall(state.QuestionsPerGame);
        }

        work.Questions.Remove(question);
        return Result.Success();
    }
}
