using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Common;
using BidQuiz.Application.Options;
using BidQuiz.Domain.Games;
using BidQuiz.Domain.Questions;
using BidQuiz.Domain.Teams;
using Microsoft.Extensions.Options;

namespace BidQuiz.Application.Game.InitializeGame;

/// <summary>
/// Runs once at startup, before any request: prepares the database, seeds a new one, brings an
/// old one up to date, and restores the deadline of a question that was open when the server stopped.
/// </summary>
public sealed class InitializeGameHandler(
    IDatabaseSchema schema,
    IUnitOfWorkFactory unitOfWork,
    GameSeeder seeder,
    IQuestionPicker picker,
    QuestionDeadline deadline,
    IOptions<GameOptions> options)
{
    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        var change = await schema.EnsureAsync(cancellationToken);
        await using var work = await unitOfWork.BeginAsync(cancellationToken);

        // Not Teams: the host may delete every team, and that must not bring the seed back.
        var state = await work.Games.FindAsync(cancellationToken);
        if (state is null)
        {
            state = await SeedAsync(work, cancellationToken);
        }
        else if (change == SchemaChange.AddedTeamRoster)
        {
            await UpgradeAsync(work, state, cancellationToken);
        }

        deadline.Set(state.OpenDeadline);
    }

    private async Task<GameState> SeedAsync(IUnitOfWork work, CancellationToken cancellationToken)
    {
        await seeder.AddMissingAsync(work, cancellationToken);

        var game = options.Value;
        var state = GameState.Create(
            GameLimits.ClampQuestionsPerGame(game.QuestionsPerGame),
            GameLimits.ClampTeamsPerGame(game.TeamsPerGame));
        work.Games.Add(state);
        await work.SaveChangesAsync(cancellationToken);

        QuestionDeck.Deal(await work.Questions.ListAsync(cancellationToken), state.QuestionsPerGame, picker);
        TeamRoster.Apply(state, await work.Teams.ListAsync(cancellationToken));
        await work.SaveChangesAsync(cancellationToken);
        return state;
    }

    /// <summary>Keeps the teams, questions and any game in progress of a database from before team counts.</summary>
    private async Task UpgradeAsync(IUnitOfWork work, GameState state, CancellationToken cancellationToken)
    {
        await seeder.AddMissingAsync(work, cancellationToken);
        state.SetQuestionsPerGame(GameLimits.ClampQuestionsPerGame(state.QuestionsPerGame));
        await work.SaveChangesAsync(cancellationToken);

        if (!await work.Questions.AnyAskedAsync(cancellationToken))
        {
            QuestionDeck.Deal(await work.Questions.ListAsync(cancellationToken), state.QuestionsPerGame, picker);
        }

        TeamRoster.Apply(state, await work.Teams.ListAsync(cancellationToken));
        await work.SaveChangesAsync(cancellationToken);
    }
}
