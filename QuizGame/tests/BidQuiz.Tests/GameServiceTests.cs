using BidQuiz.Server.Data;
using BidQuiz.Server.Domain;
using BidQuiz.Server.Game;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace BidQuiz.Tests;

public sealed class GameServiceTests : IAsyncLifetime
{
    private const int TeamA = 1;
    private const int TeamB = 2;
    private const int Q1 = 1;
    private const int Q2 = 2;

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly RecordingNotifier _notifier = new();
    private readonly PresenceTracker _presence = new();
    private IDbContextFactory<GameDbContext> _dbFactory = null!;
    private GameService _game = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<GameDbContext>().UseSqlite(_connection).Options;
        _dbFactory = new Factory(options);

        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
        db.Teams.AddRange(
            new Team { Id = TeamA, Name = "A", Color = "#111111", BidBalance = 10000 },
            new Team { Id = TeamB, Name = "B", Color = "#222222", BidBalance = 10000 });
        db.Questions.AddRange(
            new Question { Id = Q1, Order = 1, Category = "Hollywood", Text = "One?", CorrectAnswer = "1" },
            new Question { Id = Q2, Order = 2, Category = "Bollywood", Text = "Two?", CorrectAnswer = "2" });
        db.GameStates.Add(new GameState { QuestionsPerGame = 2 });
        await db.SaveChangesAsync();

        _game = NewGame();
        await _game.InitializeAsync();
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    private GameService NewGame()
    {
        var scoring = new HighestBidScoringPolicy(Options.Create(new ScoringOptions()));
        return new GameService(
            _dbFactory,
            _notifier,
            new GameGate(),
            new Scoreboard(_presence, scoring),
            scoring,
            new QuestionDeck(new InOrderPicker()),
            Options.Create(new GameOptions { QuestionSeconds = 30 }),
            _time);
    }

    private async Task<Team> TeamAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Teams.SingleAsync(t => t.Id == id);
    }

    private async Task SetTeamAsync(int id, int bidBalance, int wallet)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Teams.Where(t => t.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(t => t.BidBalance, bidBalance)
            .SetProperty(t => t.Wallet, wallet));
    }

    private async Task<int> RevealedBidIdAsync(int teamId, int amount = 3000)
    {
        await _game.StartQuestionAsync(Q1);
        await _game.SubmitBidAsync(teamId, "answer", amount);
        await _game.RevealBidsAsync();
        return _notifier.RevealedBids.Single().Id;
    }

    [Fact]
    public async Task BidBeforeDeadline_IsAccepted()
    {
        await _game.StartQuestionAsync(Q1);
        _time.Advance(TimeSpan.FromSeconds(29.9));

        var bid = await _game.SubmitBidAsync(TeamA, "answer", 1000);

        Assert.Equal(1000, bid.Amount);
    }

    [Fact]
    public async Task BidAfterDeadline_IsRejectedEvenBeforeTheTickFires()
    {
        await _game.StartQuestionAsync(Q1);
        _time.Advance(TimeSpan.FromSeconds(30));

        await Assert.ThrowsAsync<GameRuleException>(() => _game.SubmitBidAsync(TeamA, "answer", 1000));
    }

    [Fact]
    public async Task BidWithNoQuestionRunning_IsRejected() =>
        await Assert.ThrowsAsync<GameRuleException>(() => _game.SubmitBidAsync(TeamA, "answer", 1000));

    [Fact]
    public async Task Resubmission_LastOneWins_AndCountsOnce()
    {
        await _game.StartQuestionAsync(Q1);
        await _game.SubmitBidAsync(TeamA, "first", 1000);
        await _game.SubmitBidAsync(TeamA, "second", 2000);

        await _game.RevealBidsAsync();

        var bid = Assert.Single(_notifier.RevealedBids);
        Assert.Equal(("second", 2000), (bid.Answer, bid.Amount));
        Assert.Equal(1, _notifier.LastAnswered);
    }

    [Fact]
    public async Task BidOverBalance_IsRejected()
    {
        await SetTeamAsync(TeamA, 2000, 0);
        await _game.StartQuestionAsync(Q1);

        var ex = await Assert.ThrowsAsync<GameRuleException>(() => _game.SubmitBidAsync(TeamA, "answer", 3000));
        Assert.Contains("Bid Balance", ex.Message);
    }

    [Fact]
    public async Task TeamWithZeroBalance_CannotBid_AndIsNotCounted()
    {
        await SetTeamAsync(TeamA, 0, 0);
        await _game.StartQuestionAsync(Q1);

        await Assert.ThrowsAsync<GameRuleException>(() => _game.SubmitBidAsync(TeamA, "answer", 1000));
        Assert.Equal(1, (await _game.GetAdminSnapshotAsync()).TeamCount);
        Assert.False((await _game.GetTeamSnapshotAsync(TeamA)).Teams.Single(t => t.Id == TeamA).CanBid);
    }

    [Fact]
    public async Task Tick_ClosesOnceAtTheDeadline()
    {
        await _game.StartQuestionAsync(Q1);

        await _game.TickAsync();
        Assert.Equal(0, _notifier.TimeUpCount);

        _time.Advance(TimeSpan.FromSeconds(30));
        await _game.TickAsync();
        await _game.TickAsync();

        Assert.Equal(1, _notifier.TimeUpCount);
    }

    [Fact]
    public async Task RevealingEarly_ClosesBidding()
    {
        await _game.StartQuestionAsync(Q1);
        await _game.RevealBidsAsync();

        Assert.Equal(1, _notifier.TimeUpCount);
        await Assert.ThrowsAsync<GameRuleException>(() => _game.SubmitBidAsync(TeamA, "answer", 1000));
    }

    [Fact]
    public async Task RestartMidQuestion_StillHonoursTheDeadline()
    {
        await _game.StartQuestionAsync(Q1);

        var restarted = NewGame();
        await restarted.InitializeAsync();
        _time.Advance(TimeSpan.FromSeconds(30));
        await restarted.TickAsync();

        Assert.Equal(1, _notifier.TimeUpCount);
    }

    [Fact]
    public async Task RevealedBids_AreSortedByAmountDescending()
    {
        await _game.StartQuestionAsync(Q1);
        await _game.SubmitBidAsync(TeamA, "a", 1000);
        await _game.SubmitBidAsync(TeamB, "b", 9000);

        await _game.RevealBidsAsync();

        Assert.Equal([9000, 1000], _notifier.RevealedBids.Select(b => b.Amount));
    }

    [Fact]
    public async Task RevealedBids_OnlyListTeamsThatBid()
    {
        await _game.StartQuestionAsync(Q1);
        await _game.SubmitBidAsync(TeamA, "a", 1000);

        await _game.RevealBidsAsync();

        Assert.Equal(TeamA, Assert.Single(_notifier.RevealedBids).TeamId);
    }

    [Fact]
    public async Task Judge_ThumbsUp_OnHighestBid_AddsBidAndOnePercent()
    {
        var bidId = await RevealedBidIdAsync(TeamA);

        await _game.JudgeAsync(bidId, Verdict.Up);

        var team = await TeamAsync(TeamA);
        Assert.Equal((13000, 30), (team.BidBalance, team.Wallet));
    }

    [Fact]
    public async Task Judge_ThumbsDown_OnHighestBid_TakesBidAndOnePercent()
    {
        await SetTeamAsync(TeamA, 10000, 100);
        var bidId = await RevealedBidIdAsync(TeamA);

        await _game.JudgeAsync(bidId, Verdict.Down);

        var team = await TeamAsync(TeamA);
        Assert.Equal((7000, 70), (team.BidBalance, team.Wallet));
    }

    [Fact]
    public async Task Judge_LowerBid_MovesNoMoney()
    {
        await _game.StartQuestionAsync(Q1);
        await _game.SubmitBidAsync(TeamA, "a", 5000);
        await _game.SubmitBidAsync(TeamB, "b", 2000);
        await _game.RevealBidsAsync();
        var lower = _notifier.RevealedBids.Single(b => b.TeamId == TeamB);

        await _game.JudgeAsync(lower.Id, Verdict.Down);

        var team = await TeamAsync(TeamB);
        Assert.Equal((10000, 0), (team.BidBalance, team.Wallet));
    }

    [Fact]
    public async Task Judge_TiedHighestBids_CorrectTeamWins_WrongTeamPays()
    {
        await SetTeamAsync(TeamB, 10000, 100);
        await _game.StartQuestionAsync(Q1);
        await _game.SubmitBidAsync(TeamA, "right", 4000);
        await _game.SubmitBidAsync(TeamB, "wrong", 4000);
        await _game.RevealBidsAsync();

        await _game.JudgeAsync(_notifier.RevealedBids.Single(b => b.TeamId == TeamA).Id, Verdict.Up);
        await _game.JudgeAsync(_notifier.RevealedBids.Single(b => b.TeamId == TeamB).Id, Verdict.Down);

        var (a, b) = (await TeamAsync(TeamA), await TeamAsync(TeamB));
        Assert.Equal((14000, 40), (a.BidBalance, a.Wallet));
        Assert.Equal((6000, 60), (b.BidBalance, b.Wallet));
    }

    [Fact]
    public async Task Judge_SameVerdictTwice_CountsOnce()
    {
        var bidId = await RevealedBidIdAsync(TeamA);

        await _game.JudgeAsync(bidId, Verdict.Up);
        await _game.JudgeAsync(bidId, Verdict.Up);

        var team = await TeamAsync(TeamA);
        Assert.Equal((13000, 30), (team.BidBalance, team.Wallet));
    }

    [Fact]
    public async Task Judge_ChangingVerdict_UndoesThenReapplies()
    {
        await SetTeamAsync(TeamA, 10000, 100);
        var bidId = await RevealedBidIdAsync(TeamA);

        await _game.JudgeAsync(bidId, Verdict.Up);
        await _game.JudgeAsync(bidId, Verdict.Down);
        var down = await TeamAsync(TeamA);
        Assert.Equal((7000, 70), (down.BidBalance, down.Wallet));

        await _game.JudgeAsync(bidId, Verdict.Up);
        var up = await TeamAsync(TeamA);
        Assert.Equal((13000, 130), (up.BidBalance, up.Wallet));
    }

    [Fact]
    public async Task Judge_ChangingVerdict_GivesBackOnlyWhatWasTaken_WhenTheWalletStoppedAtZero()
    {
        await SetTeamAsync(TeamA, 10000, 10);
        var bidId = await RevealedBidIdAsync(TeamA);

        await _game.JudgeAsync(bidId, Verdict.Down);
        var down = await TeamAsync(TeamA);
        Assert.Equal((7000, 0), (down.BidBalance, down.Wallet));

        await _game.JudgeAsync(bidId, Verdict.Up);
        var up = await TeamAsync(TeamA);
        Assert.Equal((13000, 40), (up.BidBalance, up.Wallet));
    }

    [Fact]
    public async Task Judge_WholeBalanceWrong_PutsTheTeamOutOfTheGame_UntilReversed()
    {
        await SetTeamAsync(TeamA, 3000, 0);
        var bidId = await RevealedBidIdAsync(TeamA);

        await _game.JudgeAsync(bidId, Verdict.Down);
        Assert.False(_notifier.LastScores.Single(t => t.Id == TeamA).CanBid);

        await _game.JudgeAsync(bidId, Verdict.Up);
        Assert.True(_notifier.LastScores.Single(t => t.Id == TeamA).CanBid);
    }

    [Fact]
    public async Task Judge_BeforeReveal_IsRejected()
    {
        await _game.StartQuestionAsync(Q1);
        await _game.SubmitBidAsync(TeamA, "answer", 1000);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var bidId = await db.Bids.Select(b => b.Id).SingleAsync();

        await Assert.ThrowsAsync<GameRuleException>(() => _game.JudgeAsync(bidId, Verdict.Up));
    }

    [Fact]
    public async Task AskedQuestion_CannotBeAskedAgain()
    {
        await _game.StartQuestionAsync(Q1);
        await _game.RevealBidsAsync();

        await Assert.ThrowsAsync<GameRuleException>(() => _game.StartQuestionAsync(Q1));
    }

    [Fact]
    public async Task StartingWhileAnotherIsRunning_IsRejected()
    {
        await _game.StartQuestionAsync(Q1);

        await Assert.ThrowsAsync<GameRuleException>(() => _game.StartQuestionAsync(Q2));
    }

    [Fact]
    public async Task QuestionLeftInTheBank_CannotBeStarted()
    {
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            await db.Questions.Where(q => q.Id == Q2).ExecuteUpdateAsync(s => s.SetProperty(q => q.Order, (int?)null));
        }

        await Assert.ThrowsAsync<GameRuleException>(() => _game.StartQuestionAsync(Q2));
    }

    [Fact]
    public async Task ResetGame_ClearsScoresBids_AndDealsTheBoardAgain()
    {
        var bidId = await RevealedBidIdAsync(TeamA);
        await _game.JudgeAsync(bidId, Verdict.Up);

        await _game.ResetGameAsync();

        var team = await TeamAsync(TeamA);
        var snapshot = await _game.GetAdminSnapshotAsync();
        Assert.Equal((10000, 0), (team.BidBalance, team.Wallet));
        Assert.Equal(GameStatus.Idle, snapshot.Status);
        Assert.Equal(2, snapshot.Questions.Count);
        Assert.All(snapshot.Questions, q => Assert.False(q.Asked));
        Assert.Equal(1, _notifier.ResetCount);
        Assert.Equal(2, _notifier.LastBoard.Count);
        await using var db = await _dbFactory.CreateDbContextAsync();
        Assert.Empty(db.Bids);
    }

    [Fact]
    public async Task TeamSnapshot_RestoresQuestionDeadlineAndOwnBid()
    {
        await _game.StartQuestionAsync(Q1);
        await _game.SubmitBidAsync(TeamA, "answer", 4000);

        var snapshot = await _game.GetTeamSnapshotAsync(TeamA);

        Assert.Equal(Q1, snapshot.Question?.Id);
        Assert.Equal("Hollywood", snapshot.Question?.Category);
        Assert.Equal(_time.GetUtcNow().UtcDateTime.AddSeconds(30), snapshot.EndsAtUtc);
        Assert.Equal(new MyBidDto("answer", 4000), snapshot.MyBid);
    }

    private sealed class Factory(DbContextOptions<GameDbContext> options) : IDbContextFactory<GameDbContext>
    {
        public GameDbContext CreateDbContext() => new(options);
    }

    private sealed class InOrderPicker : IQuestionPicker
    {
        public IReadOnlyList<int> Pick(IReadOnlyList<QuestionCandidate> candidates, int count) =>
            candidates.Take(count).Select(c => c.Id).ToList();
    }

    private sealed class RecordingNotifier : IGameNotifier
    {
        public int TimeUpCount { get; private set; }
        public int ResetCount { get; private set; }
        public int LastAnswered { get; private set; }
        public IReadOnlyList<BidDto> RevealedBids { get; private set; } = [];
        public IReadOnlyList<TeamScoreDto> LastScores { get; private set; } = [];
        public IReadOnlyList<QuestionSummaryDto> LastBoard { get; private set; } = [];

        public Task QuestionStartedAsync(QuestionDto question, DateTime endsAtUtc, DateTime serverNowUtc) =>
            Task.CompletedTask;

        public Task AnswerCountChangedAsync(int answered, int total)
        {
            LastAnswered = answered;
            return Task.CompletedTask;
        }

        public Task TimeUpAsync()
        {
            TimeUpCount++;
            return Task.CompletedTask;
        }

        public Task BidsRevealedAsync(IReadOnlyList<BidDto> bids)
        {
            RevealedBids = bids;
            return Task.CompletedTask;
        }

        public Task AnswerRevealedAsync(string text) => Task.CompletedTask;

        public Task ScoresUpdatedAsync(IReadOnlyList<TeamScoreDto> teams)
        {
            LastScores = teams;
            return Task.CompletedTask;
        }

        public Task TeamPresenceChangedAsync(int teamId, bool online) => Task.CompletedTask;

        public Task GameResetAsync()
        {
            ResetCount++;
            return Task.CompletedTask;
        }

        public Task QuestionBoardChangedAsync(IReadOnlyList<QuestionSummaryDto> questions)
        {
            LastBoard = questions;
            return Task.CompletedTask;
        }

        public Task RosterChangedAsync() => Task.CompletedTask;
    }
}
