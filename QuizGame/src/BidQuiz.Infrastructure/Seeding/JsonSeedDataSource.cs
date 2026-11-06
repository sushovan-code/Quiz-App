using System.Text.Json;
using BidQuiz.Application.Abstractions.Seeding;
using Microsoft.Extensions.Hosting;

namespace BidQuiz.Infrastructure.Seeding;

/// <summary>Reads seed/teams.json and seed/questions.json from the app's content root.</summary>
public sealed class JsonSeedDataSource(IHostEnvironment environment) : ISeedDataSource
{
    private const string SeedFolder = "seed";
    private const string TeamsFile = "teams.json";
    private const string QuestionsFile = "questions.json";

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public Task<IReadOnlyList<TeamSeed>> ReadTeamsAsync(CancellationToken cancellationToken) =>
        ReadAsync<TeamSeed>(TeamsFile, cancellationToken);

    public Task<IReadOnlyList<QuestionSeed>> ReadQuestionsAsync(CancellationToken cancellationToken) =>
        ReadAsync<QuestionSeed>(QuestionsFile, cancellationToken);

    private async Task<IReadOnlyList<T>> ReadAsync<T>(string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(environment.ContentRootPath, SeedFolder, fileName);
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<T>>(stream, Json, cancellationToken)
            ?? throw new InvalidOperationException($"Seed file {path} is empty.");
    }
}
