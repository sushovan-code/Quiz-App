using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Domain.Games;

namespace BidQuiz.Application.Setup;

/// <summary>Reads the setup screen: teams in roster order, questions by category.</summary>
public static class SetupReader
{
    public static async Task<SetupResponse> ReadAsync(IUnitOfWork work, CancellationToken cancellationToken)
    {
        var state = await work.Games.GetAsync(cancellationToken);
        var teams = await work.Teams.ListAsync(cancellationToken);
        var questions = await work.Questions.ListAsync(cancellationToken);

        return new SetupResponse(
            teams.OrderBy(t => t.Id).Select(t => new TeamSetupResponse(t.Id, t.Name, t.Color, t.IsActive)).ToList(),
            questions
                .OrderBy(q => q.Category, StringComparer.Ordinal)
                .ThenBy(q => q.Id)
                .Select(q => new QuestionSetupResponse(q.Id, q.Order, q.Category, q.Text, q.CorrectAnswer, q.ImageUrl))
                .ToList(),
            state.QuestionsPerGame,
            state.TeamsPerGame,
            GameLimits.MaxTeams,
            GameLimits.MinQuestionsPerGame,
            GameLimits.MaxQuestionsPerGame,
            await work.Questions.AnyAskedAsync(cancellationToken));
    }
}
