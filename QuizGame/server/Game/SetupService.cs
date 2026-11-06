using BidQuiz.Server.Data;
using BidQuiz.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace BidQuiz.Server.Game;

/// <summary>Setup can only change before the first question: mid-game edits would orphan bids and scores.</summary>
public sealed class SetupService(
    IDbContextFactory<GameDbContext> dbFactory,
    IGameNotifier notifier,
    GameGate gate,
    Scoreboard scoreboard,
    IScoringPolicy scoring,
    QuestionDeck deck) : ISetupService
{
    private const int MaxNameLength = 40;
    private const int MaxTextLength = 500;

    public async Task<IReadOnlyList<TeamOptionDto>> ListTeamsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Teams
            .Where(t => t.Active)
            .OrderBy(t => t.Name)
            .Select(t => new TeamOptionDto(t.Id, t.Name))
            .ToListAsync();
    }

    public async Task<SetupDto> ReadAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await ToDtoAsync(db);
    }

    public Task<SetupDto> AddTeamAsync(TeamInput input) => ChangeAsync(async db =>
    {
        var teams = await db.Teams.ToListAsync();
        if (teams.Count >= GameLimits.MaxTeams)
        {
            throw new GameRuleException($"A game can have at most {GameLimits.MaxTeams} teams.");
        }

        var name = CleanName(input.Name);
        EnsureNameIsFree(teams, name, null);
        if (string.IsNullOrWhiteSpace(input.Password))
        {
            throw new GameRuleException("Please enter a team password.");
        }

        var opening = scoring.Opening;
        var team = new Team
        {
            Name = name,
            Color = TeamPalette.FirstFree(teams.Select(t => t.Color)),
            BidBalance = opening.BidBalance,
            Wallet = opening.Wallet
        };
        TeamPassword.Set(team, input.Password);
        db.Teams.Add(team);
    });

    public Task<SetupDto> UpdateTeamAsync(int teamId, TeamInput input) => ChangeAsync(async db =>
    {
        var teams = await db.Teams.ToListAsync();
        var team = teams.SingleOrDefault(t => t.Id == teamId) ?? throw new GameRuleException("Unknown team.");

        var name = CleanName(input.Name);
        EnsureNameIsFree(teams, name, teamId);
        team.Name = name;
        if (!string.IsNullOrWhiteSpace(input.Password))
        {
            TeamPassword.Set(team, input.Password);
        }
    });

    public Task<SetupDto> DeleteTeamAsync(int teamId) => ChangeAsync(async db =>
    {
        var team = await db.Teams.FindAsync(teamId) ?? throw new GameRuleException("Unknown team.");
        db.Teams.Remove(team);
    });

    public Task<SetupDto> AddQuestionAsync(QuestionInput input) => ChangeAsync(async db =>
    {
        var question = new Question { Category = "", Text = "", CorrectAnswer = "" };
        await ApplyAsync(db, question, input);
        db.Questions.Add(question);
    });

    public Task<SetupDto> UpdateQuestionAsync(int questionId, QuestionInput input) => ChangeAsync(async db =>
    {
        var question = await db.Questions.FindAsync(questionId) ?? throw new GameRuleException("Unknown question.");
        await ApplyAsync(db, question, input);
    });

    public Task<SetupDto> DeleteQuestionAsync(int questionId) => ChangeAsync(async db =>
    {
        var question = await db.Questions.FindAsync(questionId) ?? throw new GameRuleException("Unknown question.");
        var state = await db.GameStates.SingleAsync();
        if (await db.Questions.CountAsync() <= state.QuestionsPerGame)
        {
            throw new GameRuleException($"The bank needs at least {state.QuestionsPerGame} questions for this game.");
        }

        db.Questions.Remove(question);
    });

    public Task<SetupDto> SetQuestionsPerGameAsync(int count) => ChangeAsync(async db =>
    {
        if (count is < GameLimits.MinQuestionsPerGame or > GameLimits.MaxQuestionsPerGame)
        {
            throw new GameRuleException(
                $"Questions per game must be between {GameLimits.MinQuestionsPerGame} and {GameLimits.MaxQuestionsPerGame}.");
        }

        if (count > await db.Questions.CountAsync())
        {
            throw new GameRuleException("There aren't that many questions in the bank.");
        }

        var state = await db.GameStates.SingleAsync();
        state.QuestionsPerGame = count;
    });

    public Task<SetupDto> SetTeamsPerGameAsync(int count) => ChangeAsync(async db =>
    {
        if (count < 1 || count > await db.Teams.CountAsync())
        {
            throw new GameRuleException("Choose between 1 team and every team on the roster.");
        }

        var state = await db.GameStates.SingleAsync();
        state.TeamsPerGame = count;
    });

    public Task<SetupDto> DrawQuestionsAsync() => ChangeAsync(async db =>
    {
        var state = await db.GameStates.SingleAsync();
        await deck.DealAsync(db, state.QuestionsPerGame);
    });

    private async Task<SetupDto> ChangeAsync(Func<GameDbContext, Task> change)
    {
        using (await gate.EnterAsync())
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            if (await IsLockedAsync(db))
            {
                throw new GameRuleException("Reset the game before changing the setup.");
            }

            await change(db);
            await db.SaveChangesAsync();
            await RefillBoardAsync(db);
            await TeamRoster.ApplyAsync(db);
            await db.SaveChangesAsync();

            await notifier.RosterChangedAsync();
            await notifier.QuestionBoardChangedAsync(await deck.ReadBoardAsync(db));
            await notifier.ScoresUpdatedAsync(await scoreboard.ReadAsync(db));
            return await ToDtoAsync(db);
        }
    }

    /// <summary>Deals again when a change leaves the board short or over, for example a deleted question.</summary>
    private async Task RefillBoardAsync(GameDbContext db)
    {
        var state = await db.GameStates.SingleAsync();
        var expected = Math.Min(state.QuestionsPerGame, await db.Questions.CountAsync());
        if (await db.Questions.CountAsync(q => q.Order != null) == expected)
        {
            return;
        }

        await deck.DealAsync(db, state.QuestionsPerGame);
        await db.SaveChangesAsync();
    }

    private static async Task ApplyAsync(GameDbContext db, Question question, QuestionInput input)
    {
        var text = input.Text.Trim();
        var answer = input.CorrectAnswer.Trim();
        var category = input.Category.Trim();
        if (text.Length == 0 || answer.Length == 0 || category.Length == 0)
        {
            throw new GameRuleException("A question needs a category, text and a correct answer.");
        }

        if (text.Length > MaxTextLength || answer.Length > MaxTextLength || category.Length > MaxNameLength)
        {
            throw new GameRuleException("That question is too long.");
        }

        if (await db.Questions.AnyAsync(q => q.Id != question.Id && EF.Functions.Collate(q.Text, "NOCASE") == text))
        {
            throw new GameRuleException("That question is already in the bank.");
        }

        question.Category = category;
        question.Text = text;
        question.CorrectAnswer = answer;
        question.ImageUrl = string.IsNullOrWhiteSpace(input.ImageUrl) ? null : input.ImageUrl.Trim();
    }

    private static string CleanName(string name)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            throw new GameRuleException("Please enter a team name.");
        }

        if (name.Length > MaxNameLength)
        {
            throw new GameRuleException($"Team names are limited to {MaxNameLength} characters.");
        }

        return name;
    }

    private static void EnsureNameIsFree(IEnumerable<Team> teams, string name, int? ownId)
    {
        if (teams.Any(t => t.Id != ownId && string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new GameRuleException("There is already a team with that name.");
        }
    }

    private static Task<bool> IsLockedAsync(GameDbContext db) => db.Questions.AnyAsync(q => q.Asked);

    private static async Task<SetupDto> ToDtoAsync(GameDbContext db)
    {
        var state = await db.GameStates.AsNoTracking().SingleAsync();
        var teams = await db.Teams.AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(t => new TeamSetupDto(t.Id, t.Name, t.Color, t.Active))
            .ToListAsync();
        var questions = await db.Questions.AsNoTracking()
            .OrderBy(q => q.Category)
            .ThenBy(q => q.Id)
            .Select(q => new QuestionSetupDto(q.Id, q.Order, q.Category, q.Text, q.CorrectAnswer, q.ImageUrl))
            .ToListAsync();

        return new SetupDto(
            teams,
            questions,
            state.QuestionsPerGame,
            state.TeamsPerGame,
            GameLimits.MaxTeams,
            GameLimits.MinQuestionsPerGame,
            GameLimits.MaxQuestionsPerGame,
            await IsLockedAsync(db));
    }
}
