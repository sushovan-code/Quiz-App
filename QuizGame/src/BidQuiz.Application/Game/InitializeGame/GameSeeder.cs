using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Security;
using BidQuiz.Application.Abstractions.Seeding;
using BidQuiz.Domain.Games;
using BidQuiz.Domain.Questions;
using BidQuiz.Domain.Scoring;
using BidQuiz.Domain.Teams;

namespace BidQuiz.Application.Game.InitializeGame;

/// <summary>Adds the seed teams and questions that the database doesn't have yet.</summary>
public sealed class GameSeeder(ISeedDataSource seeds, IPasswordHasher hasher, IScoringPolicy scoring)
{
    public async Task AddMissingAsync(IUnitOfWork work, CancellationToken cancellationToken)
    {
        await AddMissingTeamsAsync(work, cancellationToken);
        await AddMissingQuestionsAsync(work, cancellationToken);
        await work.SaveChangesAsync(cancellationToken);
    }

    private async Task AddMissingTeamsAsync(IUnitOfWork work, CancellationToken cancellationToken)
    {
        var teams = (await work.Teams.ListAsync(cancellationToken)).ToList();
        var missing = (await seeds.ReadTeamsAsync(cancellationToken))
            .Where(seed => teams.All(t => !string.Equals(t.Name, seed.Name, StringComparison.Ordinal)));

        foreach (var seed in missing)
        {
            if (teams.Count >= GameLimits.MaxTeams)
            {
                break;
            }

            var color = TeamPalette.FirstFree(teams.Select(t => t.Color));
            var team = Team.Create(seed.Name, color, hasher.Hash(seed.Password), scoring.Opening);
            work.Teams.Add(team);
            teams.Add(team);
        }
    }

    private async Task AddMissingQuestionsAsync(IUnitOfWork work, CancellationToken cancellationToken)
    {
        var texts = (await work.Questions.ListTextsAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        foreach (var seed in await seeds.ReadQuestionsAsync(cancellationToken))
        {
            if (!texts.Contains(seed.Text))
            {
                work.Questions.Add(Question.Create(
                    new QuestionContent(seed.Category, seed.Text, seed.CorrectAnswer, seed.ImageUrl)));
            }
        }
    }
}
