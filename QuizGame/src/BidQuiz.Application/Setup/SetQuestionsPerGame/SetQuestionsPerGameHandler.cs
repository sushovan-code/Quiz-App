using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Games;

namespace BidQuiz.Application.Setup.SetQuestionsPerGame;

public sealed class SetQuestionsPerGameHandler(SetupChanges changes)
{
    public Task<Result<SetupResponse>> HandleAsync(SetQuestionsPerGame command, CancellationToken cancellationToken) =>
        changes.ApplyAsync((work, token) => SetAsync(work, command.Count, token), cancellationToken);

    private static async Task<Result> SetAsync(IUnitOfWork work, int count, CancellationToken cancellationToken)
    {
        if (count is < GameLimits.MinQuestionsPerGame or > GameLimits.MaxQuestionsPerGame)
        {
            return GameErrors.QuestionsPerGameOutOfRange;
        }

        if (count > await work.Questions.CountAsync(cancellationToken))
        {
            return GameErrors.NotEnoughQuestions;
        }

        var state = await work.Games.GetAsync(cancellationToken);
        state.SetQuestionsPerGame(count);
        return Result.Success();
    }
}
