using BidQuiz.Application;
using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Presence;
using BidQuiz.Application.Abstractions.Realtime;
using BidQuiz.Application.Abstractions.Security;
using BidQuiz.Application.Abstractions.Seeding;
using BidQuiz.Application.Bids;
using BidQuiz.Application.Bids.JudgeBid;
using BidQuiz.Application.Bids.SubmitBid;
using BidQuiz.Application.Game.CloseExpiredQuestion;
using BidQuiz.Application.Game.InitializeGame;
using BidQuiz.Application.Game.ResetGame;
using BidQuiz.Application.Game.RevealBids;
using BidQuiz.Application.Game.StartQuestion;
using BidQuiz.Application.Options;
using BidQuiz.Application.Snapshots;
using BidQuiz.Application.Snapshots.GetAdminSnapshot;
using BidQuiz.Application.Snapshots.GetTeamSnapshot;
using BidQuiz.Domain.Bids;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Questions;
using BidQuiz.Infrastructure.Persistence;
using BidQuiz.Infrastructure.Persistence.Sqlite;
using BidQuiz.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BidQuiz.IntegrationTests.Game;

/// <summary>
/// One running copy of the game: the real use cases and SQLite adapter, with a fake clock and a
/// recording notifier. Two drivers on the same connection behave like a server restart.
/// </summary>
internal sealed class GameDriver : IAsyncDisposable
{
    public const int QuestionSeconds = 30;

    private readonly ServiceProvider _services;

    public GameDriver(SqliteConnection connection, TimeProvider time, IGameNotifier notifier, ITeamPresence presence)
    {
        var services = new ServiceCollection();
        services.AddSingleton(time);
        services.AddOptions<GameOptions>().Configure(options => options.QuestionSeconds = QuestionSeconds);
        services.AddOptions<ScoringOptions>();
        services.AddApplication();
        services.AddSingleton<IQuestionPicker, InOrderPicker>();
        services.AddDbContextFactory<BidQuizDbContext>(options => options.UseSqlite(connection));
        services.AddSingleton<IUnitOfWorkFactory, EfUnitOfWorkFactory>();
        services.AddSingleton<IDatabaseSchema, SqliteDatabaseSchema>();
        services.AddSingleton<ISeedDataSource, NoSeeds>();
        services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();
        services.AddSingleton<IAdminSessionStore, InMemoryAdminSessionStore>();
        services.AddSingleton(notifier);
        services.AddSingleton(presence);
        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }

    // xUnit 2 has no per-test token; nothing here is cancelled.
    private static CancellationToken Token => CancellationToken.None;

    public Task<BidQuizDbContext> OpenDatabaseAsync() =>
        _services.GetRequiredService<IDbContextFactory<BidQuizDbContext>>().CreateDbContextAsync(Token);

    public Task InitializeAsync() => Handler<InitializeGameHandler>().HandleAsync(Token);

    public Task<Result> StartQuestionAsync(int questionId) =>
        Handler<StartQuestionHandler>().HandleAsync(new StartQuestion(questionId), Token);

    public Task<Result<MyBidResponse>> SubmitBidAsync(int teamId, string answer, int amount) =>
        Handler<SubmitBidHandler>().HandleAsync(new SubmitBid(teamId, answer, amount), Token);

    public Task TickAsync() => Handler<CloseExpiredQuestionHandler>().HandleAsync(Token);

    public Task<Result> RevealBidsAsync() => Handler<RevealBidsHandler>().HandleAsync(Token);

    public Task<Result<BidResponse>> JudgeAsync(int bidId, Verdict verdict) =>
        Handler<JudgeBidHandler>().HandleAsync(new JudgeBid(bidId, verdict), Token);

    public Task ResetGameAsync() => Handler<ResetGameHandler>().HandleAsync(Token);

    public Task<AdminSnapshotResponse> GetAdminSnapshotAsync() =>
        Handler<GetAdminSnapshotHandler>().HandleAsync(Token);

    public Task<TeamSnapshotResponse> GetTeamSnapshotAsync(int teamId) =>
        Handler<GetTeamSnapshotHandler>().HandleAsync(new GetTeamSnapshot(teamId), Token);

    public ValueTask DisposeAsync() => _services.DisposeAsync();

    private T Handler<T>()
        where T : notnull => _services.GetRequiredService<T>();

    private sealed class InOrderPicker : IQuestionPicker
    {
        public IReadOnlyList<int> Pick(IReadOnlyList<QuestionCandidate> candidates, int count) =>
            candidates.Take(count).Select(c => c.Id).ToList();
    }

    private sealed class NoSeeds : ISeedDataSource
    {
        public Task<IReadOnlyList<TeamSeed>> ReadTeamsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TeamSeed>>([]);

        public Task<IReadOnlyList<QuestionSeed>> ReadQuestionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<QuestionSeed>>([]);
    }
}
