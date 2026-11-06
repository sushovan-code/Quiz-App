using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Questions;

namespace BidQuiz.Application.Setup.UpdateQuestion;

public sealed class UpdateQuestionHandler(SetupChanges changes)
{
    public Task<Result<SetupResponse>> HandleAsync(UpdateQuestion command, CancellationToken cancellationToken) =>
        changes.ApplyAsync((work, token) => UpdateAsync(work, command, token), cancellationToken);

    private static async Task<Result> UpdateAsync(
        IUnitOfWork work,
        UpdateQuestion command,
        CancellationToken cancellationToken)
    {
        var question = await work.Questions.FindAsync(command.QuestionId, cancellationToken);
        if (question is null)
        {
            return QuestionErrors.UnknownQuestion;
        }

        var request = command.Question;
        var content = QuestionContent.Create(request.Category, request.Text, request.CorrectAnswer, request.ImageUrl);
        if (content.IsFailure)
        {
            return content;
        }

        if (await work.Questions.ExistsWithTextAsync(content.Value.Text, question.Id, cancellationToken))
        {
            return QuestionErrors.Duplicate;
        }

        question.Edit(content.Value);
        return Result.Success();
    }
}
