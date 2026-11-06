using BidQuiz.Application.Bids;
using BidQuiz.Domain.Bids;
using BidQuiz.Domain.Games;
using BidQuiz.Domain.Questions;
using BidQuiz.Domain.Scoring;
using BidQuiz.Domain.Teams;
using BidQuiz.Infrastructure.Presence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace BidQuiz.IntegrationTests.Game;

/// <summary>Plays the game through its use cases on an in-memory SQLite database.</summary>
public sealed class GameFlowTests : IAsyncLifetime
{
    private const int TeamA = 1;
    private const int TeamB = 2;
    private const int Q1 = 1;
    private const int Q2 = 2;

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly RecordingNotifier _notifier = new();
    private readonly InMemoryTeamPresence _presence = new();
    private readonly List<GameDriver> _drivers = [];
    private GameDriver _game = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _game = NewGame();

        await using (var db = await _game.OpenDatabaseAsync())
        {
            await db.Database.EnsureCreatedAsync();
            var opening = new Balances(10000, 0);
            db.Teams.Add(Team.Create("A", "#111111", "", opening));
            db.Teams.Add(Team.Create("B", "#222222", "", opening));
            await db.SaveChangesAsync();

            var one = Question.Create(new QuestionContent("Hollywood", "One?", "1", null));
            var two = Question.Create(new QuestionContent("Bollywood", "Two?", "2", null));
            one.PlaceInGame(1);
            two.PlaceInGame(2);
            db.Questions.Add(one);
            db.Questions.Add(two);
            db.GameStates.Add(GameState.Create(questionsPerGame: 2, GameLimits.DefaultTeamsPerGame));
            await db.SaveChangesAsync();
        }

        await _game.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        foreach (var driver in _drivers)
        {
            await driver.DisposeAsync();
        }

        await _connection.DisposeAsync();
    }

    private GameDriver NewGame()
    {
        var driver = new GameDriver(_connection, _time, _notifier, _presence);
        _drivers.Add(driver);
        return driver;
    }

    private async Task<Team> TeamAsync(int id)
    {
        await using var db = await _game.OpenDatabaseAsync();
        return await db.Teams.SingleAsync(t => t.Id == id);
    }

    private async Task SetTeamAsync(int id, int bidBalance, int wallet)
    {
        await using var db = await _game.OpenDatabaseAsync();
        await db.Teams.Where(t => t.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(t => t.BidBalance, bidBalance)
            .SetProperty(t => t.Wallet, wallet));
    }

    private async Task<int> RevealedBidIdAsync(int teamId, int amount = 3000)
    {
        await _game.StartQuestionAsync(Q1).SucceedsAsync();
        await _game.SubmitBidAsync(teamId, "answer", amount).SucceedsAsync();
        await _game.RevealBidsAsync().SucceedsAsync();
        return _notifier.RevealedBids.Single().Id;
    }

    private Task<BidResponse> JudgeAsync(int bidId, Verdict verdict) => _game.JudgeAsync(bidId, verdict).SucceedsAsync();

    [Fact]
    public async Task BidBeforeDeadline_IsAccepted()
    {
        await _game.StartQuestionAsync(Q1).SucceedsAsync();
        _time.Advance(TimeSpan.FromSeconds(29.9));

        var bid = await _game.SubmitBidAsync(TeamA, "answer", 1000).SucceedsAsync();

        Assert.Equal(1000, bid.Amount);
    }

    [Fact]
    public async Task BidAfterDeadline_IsRejectedEvenBeforeTheTickFires()
    {
        await _game.StartQuestionAsync(Q1).SucceedsAsync();
        _time.Advance(TimeSpan.FromSeconds(30));

        await _game.SubmitBidAsync(TeamA, "answer", 1000).FailsAsync();
    }

    [Fact]
    public async Task BidWithNoQuestionRunning_IsRejected() =>
        await _game.SubmitBidAsync(TeamA, "answer", 1000).FailsAsync();

    [Fact]
    public async Task Resubmission_LastOneWins_AndCountsOnce()
    {
        await _game.StartQuestionAsync(Q1).SucceedsAsync();
        await _game.SubmitBidAsync(TeamA, "first", 1000).SucceedsAsync();
        await _game.SubmitBidAsync(TeamA, "second", 2000).SucceedsAsync();

        await _game.RevealBidsAsync().SucceedsAsync();

        var bid = Assert.Single(_notifier.RevealedBids);
        Assert.Equal(("second", 2000), (bid.Answer, bid.Amount));
        Assert.Equal(1, _notifier.LastAnswered);
    }

    [Fact]
    public async Task BidOverBalance_IsRejected()
    {
        await SetTeamAsync(TeamA, 2000, 0);
        await _game.StartQuestionAsync(Q1).SucceedsAsync();

        var error = await _game.SubmitBidAsync(TeamA, "answer", 3000).FailsAsync();
        Assert.Contains("Bid Balance", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TeamWithZeroBalance_CannotBid_AndIsNotCounted()
    {
        await SetTeamAsync(TeamA, 0, 0);
        await _game.StartQuestionAsync(Q1).SucceedsAsync();

        await _game.SubmitBidAsync(TeamA, "answer", 1000).FailsAsync();
        Assert.Equal(1, (await _game.GetAdminSnapshotAsync()).TeamCount);
        Assert.False((await _game.GetTeamSnapshotAsync(TeamA)).Teams.Single(t => t.Id == TeamA).CanBid);
    }

    [Fact]
    public async Task Tick_ClosesOnceAtTheDeadline()
    {
        await _game.StartQuestionAsync(Q1).SucceedsAsync();

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
        await _game.StartQuestionAsync(Q1).SucceedsAsync();
        await _game.RevealBidsAsync().SucceedsAsync();

        Assert.Equal(1, _notifier.TimeUpCount);
        await _game.SubmitBidAsync(TeamA, "answer", 1000).FailsAsync();
    }

    [Fact]
    public async Task RestartMidQuestion_StillHonoursTheDeadline()
    {
        await _game.StartQuestionAsync(Q1).SucceedsAsync();

        var restarted = NewGame();
        await restarted.InitializeAsync();
        _time.Advance(TimeSpan.FromSeconds(30));
        await restarted.TickAsync();

        Assert.Equal(1, _notifier.TimeUpCount);
    }

    [Fact]
    public async Task RevealedBids_AreSortedByAmountDescending()
    {
        await _game.StartQuestionAsync(Q1).SucceedsAsync();
        await _game.SubmitBidAsync(TeamA, "a", 1000).SucceedsAsync();
        await _game.SubmitBidAsync(TeamB, "b", 9000).SucceedsAsync();

        await _game.RevealBidsAsync().SucceedsAsync();

        Assert.Equal([9000, 1000], _notifier.RevealedBids.Select(b => b.Amount));
    }

    [Fact]
    public async Task RevealedBids_OnlyListTeamsThatBid()
    {
        await _game.StartQuestionAsync(Q1).SucceedsAsync();
        await _game.SubmitBidAsync(TeamA, "a", 1000).SucceedsAsync();

        await _game.RevealBidsAsync().SucceedsAsync();

        Assert.Equal(TeamA, Assert.Single(_notifier.RevealedBids).TeamId);
    }

    [Fact]
    public async Task Judge_ThumbsUp_OnHighestBid_AddsBidAndOnePercent()
    {
        var bidId = await RevealedBidIdAsync(TeamA);

        await JudgeAsync(bidId, Verdict.Up);

        var team = await TeamAsync(TeamA);
        Assert.Equal((13000, 30), (team.BidBalance, team.Wallet));
    }

    [Fact]
    public async Task Judge_ThumbsDown_OnHighestBid_TakesBidAndOnePercent()
    {
        await SetTeamAsync(TeamA, 10000, 100);
        var bidId = await RevealedBidIdAsync(TeamA);

        await JudgeAsync(bidId, Verdict.Down);

        var team = await TeamAsync(TeamA);
        Assert.Equal((7000, 70), (team.BidBalance, team.Wallet));
    }

    [Fact]
    public async Task Judge_LowerBid_MovesNoMoney()
    {
        await _game.StartQuestionAsync(Q1).SucceedsAsync();
        await _game.SubmitBidAsync(TeamA, "a", 5000).SucceedsAsync();
        await _game.SubmitBidAsync(TeamB, "b", 2000).SucceedsAsync();
        await _game.RevealBidsAsync().SucceedsAsync();
        var lower = _notifier.RevealedBids.Single(b => b.TeamId == TeamB);

        await JudgeAsync(lower.Id, Verdict.Down);

        var team = await TeamAsync(TeamB);
        Assert.Equal((10000, 0), (team.BidBalance, team.Wallet));
    }

    [Fact]
    public async Task Judge_TiedHighestBids_CorrectTeamWins_WrongTeamPays()
    {
        await SetTeamAsync(TeamB, 10000, 100);
        await _game.StartQuestionAsync(Q1).SucceedsAsync();
        await _game.SubmitBidAsync(TeamA, "right", 4000).SucceedsAsync();
        await _game.SubmitBidAsync(TeamB, "wrong", 4000).SucceedsAsync();
        await _game.RevealBidsAsync().SucceedsAsync();

        await JudgeAsync(_notifier.RevealedBids.Single(b => b.TeamId == TeamA).Id, Verdict.Up);
        await JudgeAsync(_notifier.RevealedBids.Single(b => b.TeamId == TeamB).Id, Verdict.Down);

        var (a, b) = (await TeamAsync(TeamA), await TeamAsync(TeamB));
        Assert.Equal((14000, 40), (a.BidBalance, a.Wallet));
        Assert.Equal((6000, 60), (b.BidBalance, b.Wallet));
    }

    [Fact]
    public async Task Judge_SameVerdictTwice_CountsOnce()
    {
        var bidId = await RevealedBidIdAsync(TeamA);

        await JudgeAsync(bidId, Verdict.Up);
        await JudgeAsync(bidId, Verdict.Up);

        var team = await TeamAsync(TeamA);
        Assert.Equal((13000, 30), (team.BidBalance, team.Wallet));
    }

    [Fact]
    public async Task Judge_ChangingVerdict_UndoesThenReapplies()
    {
        await SetTeamAsync(TeamA, 10000, 100);
        var bidId = await RevealedBidIdAsync(TeamA);

        await JudgeAsync(bidId, Verdict.Up);
        await JudgeAsync(bidId, Verdict.Down);
        var down = await TeamAsync(TeamA);
        Assert.Equal((7000, 70), (down.BidBalance, down.Wallet));

        await JudgeAsync(bidId, Verdict.Up);
        var up = await TeamAsync(TeamA);
        Assert.Equal((13000, 130), (up.BidBalance, up.Wallet));
    }

    [Fact]
    public async Task Judge_ChangingVerdict_GivesBackOnlyWhatWasTaken_WhenTheWalletStoppedAtZero()
    {
        await SetTeamAsync(TeamA, 10000, 10);
        var bidId = await RevealedBidIdAsync(TeamA);

        await JudgeAsync(bidId, Verdict.Down);
        var down = await TeamAsync(TeamA);
        Assert.Equal((7000, 0), (down.BidBalance, down.Wallet));

        await JudgeAsync(bidId, Verdict.Up);
        var up = await TeamAsync(TeamA);
        Assert.Equal((13000, 40), (up.BidBalance, up.Wallet));
    }

    [Fact]
    public async Task Judge_WholeBalanceWrong_PutsTheTeamOutOfTheGame_UntilReversed()
    {
        await SetTeamAsync(TeamA, 3000, 0);
        var bidId = await RevealedBidIdAsync(TeamA);

        await JudgeAsync(bidId, Verdict.Down);
        Assert.False(_notifier.LastScores.Single(t => t.Id == TeamA).CanBid);

        await JudgeAsync(bidId, Verdict.Up);
        Assert.True(_notifier.LastScores.Single(t => t.Id == TeamA).CanBid);
    }

    [Fact]
    public async Task Judge_BeforeReveal_IsRejected()
    {
        await _game.StartQuestionAsync(Q1).SucceedsAsync();
        await _game.SubmitBidAsync(TeamA, "answer", 1000).SucceedsAsync();
        await using var db = await _game.OpenDatabaseAsync();
        var bidId = await db.Bids.Select(b => b.Id).SingleAsync();

        await _game.JudgeAsync(bidId, Verdict.Up).FailsAsync();
    }

    [Fact]
    public async Task AskedQuestion_CannotBeAskedAgain()
    {
        await _game.StartQuestionAsync(Q1).SucceedsAsync();
        await _game.RevealBidsAsync().SucceedsAsync();

        await _game.StartQuestionAsync(Q1).FailsAsync();
    }

    [Fact]
    public async Task StartingWhileAnotherIsRunning_IsRejected()
    {
        await _game.StartQuestionAsync(Q1).SucceedsAsync();

        await _game.StartQuestionAsync(Q2).FailsAsync();
    }

    [Fact]
    public async Task QuestionLeftInTheBank_CannotBeStarted()
    {
        await using (var db = await _game.OpenDatabaseAsync())
        {
            await db.Questions.Where(q => q.Id == Q2).ExecuteUpdateAsync(s => s.SetProperty(q => q.Order, (int?)null));
        }

        await _game.StartQuestionAsync(Q2).FailsAsync();
    }

    [Fact]
    public async Task ResetGame_ClearsScoresBids_AndDealsTheBoardAgain()
    {
        var bidId = await RevealedBidIdAsync(TeamA);
        await JudgeAsync(bidId, Verdict.Up);

        await _game.ResetGameAsync();

        var team = await TeamAsync(TeamA);
        var snapshot = await _game.GetAdminSnapshotAsync();
        Assert.Equal((10000, 0), (team.BidBalance, team.Wallet));
        Assert.Equal(GameStatus.Idle, snapshot.Status);
        Assert.Equal(2, snapshot.Questions.Count);
        Assert.All(snapshot.Questions, q => Assert.False(q.Asked));
        Assert.Equal(1, _notifier.ResetCount);
        Assert.Equal(2, _notifier.LastBoard.Count);
        await using var db = await _game.OpenDatabaseAsync();
        Assert.Empty(db.Bids);
    }

    [Fact]
    public async Task TeamSnapshot_RestoresQuestionDeadlineAndOwnBid()
    {
        await _game.StartQuestionAsync(Q1).SucceedsAsync();
        await _game.SubmitBidAsync(TeamA, "answer", 4000).SucceedsAsync();

        var snapshot = await _game.GetTeamSnapshotAsync(TeamA);

        Assert.Equal(Q1, snapshot.Question?.Id);
        Assert.Equal("Hollywood", snapshot.Question?.Category);
        Assert.Equal(_time.GetUtcNow().UtcDateTime.AddSeconds(GameDriver.QuestionSeconds), snapshot.EndsAtUtc);
        Assert.Equal(new MyBidResponse("answer", 4000), snapshot.MyBid);
    }
}
