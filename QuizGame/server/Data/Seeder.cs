using System.Text.Json;
using BidQuiz.Server.Domain;
using BidQuiz.Server.Game;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BidQuiz.Server.Data;

public static class Seeder
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private sealed record TeamSeed(string Name, string Password);
    private sealed record QuestionSeed(string Category, string Text, string CorrectAnswer, string? ImageUrl);

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<GameDbContext>>();
        var env = services.GetRequiredService<IWebHostEnvironment>();
        var scoring = services.GetRequiredService<IScoringPolicy>();
        var deck = services.GetRequiredService<QuestionDeck>();
        var game = services.GetRequiredService<IOptions<GameOptions>>().Value;

        await using var db = await dbFactory.CreateDbContextAsync();
        EnsureDatabaseDirectory(db.Database.GetConnectionString());
        await db.Database.EnsureCreatedAsync();

        var seedDir = Path.Combine(env.ContentRootPath, "seed");

        // Not Teams: the host may delete every team, and that must not bring the seed back.
        if (await db.GameStates.AnyAsync())
        {
            await UpgradeAsync(db, deck, scoring, seedDir);
            return;
        }

        await AddMissingAsync(db, scoring, seedDir);

        var questionsPerGame = ClampQuestions(game.QuestionsPerGame);
        db.GameStates.Add(new GameState
        {
            Status = GameStatus.Idle,
            QuestionsPerGame = questionsPerGame,
            TeamsPerGame = Math.Clamp(game.TeamsPerGame, 1, GameLimits.MaxTeams)
        });
        await db.SaveChangesAsync();

        await deck.DealAsync(db, questionsPerGame);
        await TeamRoster.ApplyAsync(db);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Brings a database from before configurable team counts up to date, keeping its teams,
    /// questions and any game in progress. Later model changes should move to EF Core migrations.
    /// </summary>
    private static async Task UpgradeAsync(
        GameDbContext db,
        QuestionDeck deck,
        IScoringPolicy scoring,
        string seedDir)
    {
        var columns = await db.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('Teams')")
            .ToListAsync();
        if (columns.Contains(nameof(Team.Active)))
            return;

        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Teams ADD COLUMN Active INTEGER NOT NULL DEFAULT 1");
        await db.Database.ExecuteSqlRawAsync(
            $"ALTER TABLE GameStates ADD COLUMN TeamsPerGame INTEGER NOT NULL DEFAULT {GameLimits.DefaultTeamsPerGame}");

        await AddMissingAsync(db, scoring, seedDir);

        var state = await db.GameStates.SingleAsync();
        state.QuestionsPerGame = ClampQuestions(state.QuestionsPerGame);
        await db.SaveChangesAsync();

        if (!await db.Questions.AnyAsync(q => q.Asked))
            await deck.DealAsync(db, state.QuestionsPerGame);
        await TeamRoster.ApplyAsync(db);
        await db.SaveChangesAsync();
    }

    private static async Task AddMissingAsync(GameDbContext db, IScoringPolicy scoring, string seedDir)
    {
        var teamSeeds = await ReadAsync<TeamSeed>(Path.Combine(seedDir, "teams.json"));
        var questionSeeds = await ReadAsync<QuestionSeed>(Path.Combine(seedDir, "questions.json"));

        var teams = await db.Teams.ToListAsync();
        var questionTexts = await db.Questions.Select(q => q.Text).ToListAsync();

        var opening = scoring.Opening;
        foreach (var seed in teamSeeds.Where(s => teams.All(t => t.Name != s.Name)))
        {
            if (teams.Count >= GameLimits.MaxTeams)
                break;

            var team = new Team
            {
                Name = seed.Name,
                Color = TeamPalette.FirstFree(teams.Select(t => t.Color)),
                BidBalance = opening.BidBalance,
                Wallet = opening.Wallet
            };
            TeamPassword.Set(team, seed.Password);
            db.Teams.Add(team);
            teams.Add(team);
        }

        db.Questions.AddRange(questionSeeds.Where(s => !questionTexts.Contains(s.Text)).Select(q => new Question
        {
            Category = q.Category,
            Text = q.Text,
            CorrectAnswer = q.CorrectAnswer,
            ImageUrl = q.ImageUrl
        }));
        await db.SaveChangesAsync();
    }

    private static int ClampQuestions(int count) =>
        Math.Clamp(count, GameLimits.MinQuestionsPerGame, GameLimits.MaxQuestionsPerGame);

    private static void EnsureDatabaseDirectory(string? connectionString)
    {
        var directory = Path.GetDirectoryName(new SqliteConnectionStringBuilder(connectionString).DataSource);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    private static async Task<List<T>> ReadAsync<T>(string path)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<T>>(stream, Json)
            ?? throw new InvalidOperationException($"Seed file {path} is empty.");
    }
}
