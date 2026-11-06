using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BidQuiz.Server.Domain;
using BidQuiz.Server.Game;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace BidQuiz.Tests;

/// <summary>Runs the real hub, seed data and group routing end to end.</summary>
public sealed class HubSecurityTests : IAsyncLifetime
{
    private const string AdminPassword = "test-admin";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"bidquiz-{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program> _factory = null!;

    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseSetting("ConnectionStrings:Game", $"Data Source={_dbPath};Pooling=False");
            host.UseSetting("Game:AdminPassword", AdminPassword);
        });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        File.Delete(_dbPath);
    }

    private async Task<HubConnection> ConnectAsync(ConcurrentBag<string>? received = null)
    {
        var server = _factory.Server;
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "hubs/game"), options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            })
            .AddJsonProtocol(options =>
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

        if (received is not null)
        {
            foreach (var name in new[] { "BidsRevealed", "AnswerRevealed", "AnswerCountChanged", "QuestionBoardChanged" })
            {
                connection.On<JsonElement>(name, payload => received.Add($"{name}:{payload}"));
            }
        }

        await connection.StartAsync();
        return connection;
    }

    private async Task<TeamOptionDto> FirstTeamAsync()
    {
        using var http = _factory.CreateClient();
        var teams = await http.GetFromJsonAsync<List<TeamOptionDto>>("/api/teams");
        return teams!.Single(t => t.Name == "The Projectionists");
    }

    [Theory]
    [InlineData("StartQuestion", 1)]
    [InlineData("RevealBids")]
    [InlineData("RevealAnswer")]
    [InlineData("ResetGame")]
    [InlineData("GetSetup")]
    [InlineData("DrawQuestions")]
    [InlineData("DeleteTeam", 1)]
    [InlineData("DeleteQuestion", 1)]
    [InlineData("SetQuestionsPerGame", 15)]
    [InlineData("SetTeamsPerGame", 5)]
    public async Task AdminMethods_AreRejectedForTeams(string method, params object[] args)
    {
        await using var team = await ConnectAsync();
        var option = await FirstTeamAsync();
        await team.InvokeAsync<TeamSession>("JoinTeam", option.Id, "reel-1");

        var ex = await Assert.ThrowsAsync<HubException>(() => team.InvokeCoreAsync(method, args));

        Assert.Contains("Admin only", ex.Message);
    }

    [Fact]
    public async Task Judge_IsRejectedForTeams()
    {
        await using var anonymous = await ConnectAsync();

        var ex = await Assert.ThrowsAsync<HubException>(() => anonymous.InvokeAsync<BidDto>("Judge", 1, "Up"));

        Assert.Contains("Admin only", ex.Message);
    }

    [Fact]
    public async Task WrongPasswords_AreRejected()
    {
        await using var connection = await ConnectAsync();
        var option = await FirstTeamAsync();

        await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync<TeamSession>("JoinTeam", option.Id, "nope"));
        await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync<AdminSession>("AdminLogin", "nope"));
    }

    [Fact]
    public async Task SubmitBid_RequiresLogin()
    {
        await using var anonymous = await ConnectAsync();

        await Assert.ThrowsAsync<HubException>(() => anonymous.InvokeAsync<MyBidDto>("SubmitBid", "answer", 1000));
    }

    [Fact]
    public async Task TeamClients_NeverReceiveBidsOrTheCorrectAnswer()
    {
        var teamReceived = new ConcurrentBag<string>();
        var adminReceived = new ConcurrentBag<string>();
        await using var team = await ConnectAsync(teamReceived);
        await using var admin = await ConnectAsync(adminReceived);

        var option = await FirstTeamAsync();
        await team.InvokeAsync<TeamSession>("JoinTeam", option.Id, "reel-1");
        var session = await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword);
        var setup = await admin.InvokeAsync<SetupDto>("GetSetup");
        var question = session.Snapshot.Questions[0];
        var correctAnswer = setup.Questions.Single(q => q.Id == question.Id).CorrectAnswer;

        await admin.InvokeAsync("StartQuestion", question.Id);
        await team.InvokeAsync<MyBidDto>("SubmitBid", "my answer", 2000);
        await admin.InvokeAsync("RevealBids");
        await admin.InvokeAsync("RevealAnswer");

        await WaitUntilAsync(() => adminReceived.Any(m => m.StartsWith("AnswerRevealed:", StringComparison.Ordinal)));

        // A marker sent after the admin messages proves anything bound for the team has arrived.
        var marker = new TaskCompletionSource();
        using var subscription = team.On("Marker", () => marker.SetResult());
        await _factory.Services.GetRequiredService<IHubContext<BidQuiz.Server.Hubs.GameHub>>()
            .Clients.Group("teams").SendAsync("Marker");
        await marker.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Contains(adminReceived, m => m.StartsWith("BidsRevealed:", StringComparison.Ordinal) && m.Contains("my answer"));
        Assert.Contains(adminReceived, m => m == $"AnswerRevealed:{correctAnswer}");
        Assert.Empty(teamReceived);
    }

    [Fact]
    public async Task Seed_GivesTwentyColouredTeams_TenPlaying_AndDealsTenOfAHundredQuestions()
    {
        await using var admin = await ConnectAsync();
        var session = await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword);
        var setup = await admin.InvokeAsync<SetupDto>("GetSetup");

        Assert.Equal(20, setup.Teams.Count);
        Assert.Equal(20, setup.Teams.Select(t => t.Color).Distinct().Count());
        Assert.Equal(10, setup.Teams.Count(t => t.Playing));
        Assert.Equal(10, session.Snapshot.Teams.Count);
        Assert.Equal(10, session.Snapshot.TeamCount);
        Assert.All(session.Snapshot.Teams, t => Assert.Equal((10000, 0), (t.BidBalance, t.Wallet)));

        Assert.Equal(100, setup.Questions.Count);
        Assert.Equal(100, setup.Questions.Select(q => q.CorrectAnswer).Distinct().Count());
        Assert.Equal(50, setup.Questions.Count(q => q.Category == "Hollywood"));
        Assert.Equal(50, setup.Questions.Count(q => q.Category == "Bollywood"));
        Assert.Equal(10, session.Snapshot.Questions.Count);
        Assert.Equal(Enumerable.Range(1, 10), session.Snapshot.Questions.Select(q => q.Order));
    }

    [Fact]
    public async Task Setup_CapsTheRosterAtTwenty_AndGivesANewTeamAFreeColour()
    {
        await using var admin = await ConnectAsync();
        await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword);
        var setup = await admin.InvokeAsync<SetupDto>("GetSetup");
        var input = new TeamInput("Newcomers", "secret");

        var full = await Assert.ThrowsAsync<HubException>(() => admin.InvokeAsync<SetupDto>("AddTeam", input));
        Assert.Contains("at most 20", full.Message);

        var removed = setup.Teams[3];
        await admin.InvokeAsync<SetupDto>("DeleteTeam", removed.Id);
        var after = await admin.InvokeAsync<SetupDto>("AddTeam", input);

        Assert.Equal(removed.Color, after.Teams.Single(t => t.Name == "Newcomers").Color);
    }

    [Fact]
    public async Task Setup_DeletingAPlayingTeam_BringsInTheNextOne()
    {
        await using var admin = await ConnectAsync();
        await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword);
        var setup = await admin.InvokeAsync<SetupDto>("GetSetup");

        var after = await admin.InvokeAsync<SetupDto>("DeleteTeam", setup.Teams[0].Id);

        Assert.Equal(10, after.Teams.Count(t => t.Playing));
    }

    [Fact]
    public async Task Setup_TeamsPerGame_ChoosesWhoCanSeeAndJoinTheGame()
    {
        await using var admin = await ConnectAsync();
        await using var phone = await ConnectAsync();
        await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword);
        using var http = _factory.CreateClient();

        var setup = await admin.InvokeAsync<SetupDto>("SetTeamsPerGame", 3);

        Assert.Equal(3, setup.Teams.Count(t => t.Playing));
        Assert.Equal(3, (await http.GetFromJsonAsync<List<TeamOptionDto>>("/api/teams"))!.Count);
        Assert.Equal(3, (await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword)).Snapshot.Teams.Count);

        var benched = setup.Teams.First(t => !t.Playing);
        var ex = await Assert.ThrowsAsync<HubException>(
            () => phone.InvokeAsync<TeamSession>("JoinTeam", benched.Id, "reel-4"));
        Assert.Contains("isn't playing", ex.Message);

        var raised = await admin.InvokeAsync<SetupDto>("SetTeamsPerGame", 20);
        Assert.Equal(20, raised.Teams.Count(t => t.Playing));
        await Assert.ThrowsAsync<HubException>(() => admin.InvokeAsync<SetupDto>("SetTeamsPerGame", 21));
        await Assert.ThrowsAsync<HubException>(() => admin.InvokeAsync<SetupDto>("SetTeamsPerGame", 0));
    }

    [Fact]
    public async Task BenchedTeam_CannotResumeItsSession()
    {
        await using var phone = await ConnectAsync();
        await using var admin = await ConnectAsync();
        await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword);
        var setup = await admin.InvokeAsync<SetupDto>("GetSetup");
        var session = await phone.InvokeAsync<TeamSession>("JoinTeam", setup.Teams[1].Id, "reel-2");

        await admin.InvokeAsync<SetupDto>("SetTeamsPerGame", 1);
        await using var again = await ConnectAsync();
        var ex = await Assert.ThrowsAsync<HubException>(() => again.InvokeAsync<TeamSession>("ResumeTeam", session.Token));

        Assert.Contains("Session expired", ex.Message);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(17)]
    [InlineData(25)]
    public async Task Setup_QuestionsPerGame_ResizesTheBoard(int count)
    {
        var received = new ConcurrentBag<string>();
        await using var admin = await ConnectAsync(received);
        await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword);

        var setup = await admin.InvokeAsync<SetupDto>("SetQuestionsPerGame", count);

        Assert.Equal(count, setup.QuestionsPerGame);
        Assert.Equal(count, setup.Questions.Count(q => q.Order is not null));
        await WaitUntilAsync(() => received.Any(m => m.StartsWith("QuestionBoardChanged:", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(9)]
    [InlineData(26)]
    public async Task Setup_QuestionsPerGame_StaysBetweenTenAndTwentyFive(int count)
    {
        await using var admin = await ConnectAsync();
        await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword);

        await Assert.ThrowsAsync<HubException>(() => admin.InvokeAsync<SetupDto>("SetQuestionsPerGame", count));
    }

    [Fact]
    public async Task Setup_DeletingAQuestionInPlay_RefillsTheBoard()
    {
        await using var admin = await ConnectAsync();
        var session = await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword);

        var setup = await admin.InvokeAsync<SetupDto>("DeleteQuestion", session.Snapshot.Questions[0].Id);

        Assert.Equal(99, setup.Questions.Count);
        Assert.Equal(10, setup.Questions.Count(q => q.Order is not null));
    }

    [Fact]
    public async Task Setup_BankCannotShrinkBelowTheQuestionsPerGame()
    {
        await using var admin = await ConnectAsync();
        await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword);
        var setup = await admin.InvokeAsync<SetupDto>("GetSetup");

        foreach (var question in setup.Questions.Skip(10))
        {
            await admin.InvokeAsync<SetupDto>("DeleteQuestion", question.Id);
        }

        var ex = await Assert.ThrowsAsync<HubException>(
            () => admin.InvokeAsync<SetupDto>("DeleteQuestion", setup.Questions[0].Id));
        Assert.Contains("at least 10", ex.Message);
    }

    [Fact]
    public async Task LeaveTeam_MarksTheTeamOffline_OnlyWhenItsLastDeviceLeaves()
    {
        await using var admin = await ConnectAsync();
        await using var phone = await ConnectAsync();
        await using var laptop = await ConnectAsync();
        var presence = new ConcurrentQueue<(int TeamId, bool Online)>();
        admin.On<int, bool>("TeamPresenceChanged", (id, online) => presence.Enqueue((id, online)));
        await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword);
        var team = await FirstTeamAsync();

        await phone.InvokeAsync<TeamSession>("JoinTeam", team.Id, "reel-1");
        await laptop.InvokeAsync<TeamSession>("JoinTeam", team.Id, "reel-1");
        await WaitUntilAsync(() => presence.Contains((team.Id, true)));

        await phone.InvokeAsync("LeaveTeam");
        var stillOnline = (await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword))
            .Snapshot.Teams.Single(t => t.Id == team.Id).Online;
        Assert.True(stillOnline);

        await laptop.InvokeAsync("LeaveTeam");
        await WaitUntilAsync(() => presence.Contains((team.Id, false)));
        await Assert.ThrowsAsync<HubException>(() => laptop.InvokeAsync<MyBidDto>("SubmitBid", "answer", 1000));
    }

    [Fact]
    public async Task Upgrade_KeepsAnExistingGame_AndAddsTheNewTeamsAndQuestions()
    {
        await using (var admin = await ConnectAsync())
        {
            var session = await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword);
            await admin.InvokeAsync("StartQuestion", session.Snapshot.Questions[0].Id);
        }
        await _factory.DisposeAsync();

        await using (var old = new SqliteConnection($"Data Source={_dbPath};Pooling=False"))
        {
            await old.OpenAsync();
            await using var command = old.CreateCommand();
            command.CommandText = """
                DELETE FROM Teams WHERE Id > 10;
                DELETE FROM Questions WHERE Asked = 0 AND Id > 50;
                ALTER TABLE Teams DROP COLUMN Active;
                ALTER TABLE GameStates DROP COLUMN TeamsPerGame;
                """;
            await command.ExecuteNonQueryAsync();
        }

        await InitializeAsync();
        await using var upgraded = await ConnectAsync();
        var after = await upgraded.InvokeAsync<AdminSession>("AdminLogin", AdminPassword);
        var setup = await upgraded.InvokeAsync<SetupDto>("GetSetup");

        Assert.Equal(GameStatus.Accepting, after.Snapshot.Status);
        Assert.Equal(20, setup.Teams.Count);
        Assert.Equal(20, setup.Teams.Select(t => t.Color).Distinct().Count());
        Assert.Equal(10, setup.Teams.Count(t => t.Playing));
        Assert.Equal(100, setup.Questions.Count);
    }

    [Fact]
    public async Task Setup_IsLockedOnceAGameStarts_UntilReset()
    {
        await using var admin = await ConnectAsync();
        var session = await admin.InvokeAsync<AdminSession>("AdminLogin", AdminPassword);
        await admin.InvokeAsync("StartQuestion", session.Snapshot.Questions[0].Id);

        var ex = await Assert.ThrowsAsync<HubException>(() => admin.InvokeAsync<SetupDto>("DrawQuestions"));
        Assert.Contains("Reset the game", ex.Message);

        await admin.InvokeAsync("ResetGame");
        Assert.False((await admin.InvokeAsync<SetupDto>("GetSetup")).Locked);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        Assert.True(condition());
    }
}
