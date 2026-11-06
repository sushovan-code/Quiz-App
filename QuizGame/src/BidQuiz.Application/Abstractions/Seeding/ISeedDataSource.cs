namespace BidQuiz.Application.Abstractions.Seeding;

/// <summary>The teams and questions a new database starts with.</summary>
public interface ISeedDataSource
{
    Task<IReadOnlyList<TeamSeed>> ReadTeamsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<QuestionSeed>> ReadQuestionsAsync(CancellationToken cancellationToken);
}
