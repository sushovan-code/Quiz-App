using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Questions;

namespace BidQuiz.Application.Setup.AddQuestion;

public sealed class AddQuestionHandler(SetupChanges changes)
{
    public Task<Result<SetupResponse>> HandleAsync(AddQuestion command, CancellationToken cancellationToken) =>
        changes.ApplyAsync((work, token) => AddAsync(work, command.Question, token), cancellationToken);

    private static async Task<Result> AddAsync(
        IUnitOfWork work,
        SaveQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var content = QuestionContent.Create(request.Category, request.Text, request.CorrectAnswer, request.ImageUrl);
        if (content.IsFailure)
        {
            return content;
        }

        if (await work.Questions.ExistsWithTextAsync(content.Value.Text, exceptQuestionId: null, cancellationToken))
        {
            return QuestionErrors.Duplicate;
        }

        work.Questions.Add(Question.Create(content.Value));
        return Result.Success();
    }
}
