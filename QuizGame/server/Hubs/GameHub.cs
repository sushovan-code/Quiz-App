using BidQuiz.Server.Domain;
using BidQuiz.Server.Game;
using Microsoft.AspNetCore.SignalR;

namespace BidQuiz.Server.Hubs;

public sealed class GameHub(
    IGameService game,
    IAuthService auth,
    ISetupService setup,
    IPresence presence,
    IGameNotifier notifier) : Hub
{
    public const string AdminGroup = "admin";
    public const string TeamsGroup = "teams";

    private const string TeamIdKey = "teamId";
    private const string IsAdminKey = "isAdmin";

    public async Task<TeamSession> JoinTeam(int teamId, string password)
    {
        var (id, token) = await auth.LoginTeamAsync(teamId, password);
        return new TeamSession(token, await AttachTeamAsync(id));
    }

    public async Task<TeamSession> ResumeTeam(string token)
    {
        var teamId = await auth.ResolveTeamTokenAsync(token);
        return new TeamSession(token, await AttachTeamAsync(teamId));
    }

    /// <summary>Logs this device out. The team stays online if another of its devices is connected.</summary>
    public async Task LeaveTeam()
    {
        await ReleasePresenceAsync();
        Context.Items.Remove(TeamIdKey);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, TeamsGroup);
    }

    public async Task<AdminSession> AdminLogin(string password)
    {
        var token = auth.LoginAdmin(password);
        return new AdminSession(token, await AttachAdminAsync());
    }

    public async Task<AdminSession> ResumeAdmin(string token)
    {
        auth.ValidateAdminToken(token);
        return new AdminSession(token, await AttachAdminAsync());
    }

    public Task StartQuestion(int questionId)
    {
        RequireAdmin();
        return game.StartQuestionAsync(questionId);
    }

    public Task<MyBidDto> SubmitBid(string answer, int amount)
    {
        if (Context.Items[TeamIdKey] is not int teamId)
            throw new GameRuleException("Please log in with your team first.");
        return game.SubmitBidAsync(teamId, answer, amount);
    }

    public Task RevealBids()
    {
        RequireAdmin();
        return game.RevealBidsAsync();
    }

    public Task RevealAnswer()
    {
        RequireAdmin();
        return game.RevealAnswerAsync();
    }

    public Task<BidDto> Judge(int bidId, Verdict verdict)
    {
        RequireAdmin();
        return game.JudgeAsync(bidId, verdict);
    }

    public Task ResetGame()
    {
        RequireAdmin();
        return game.ResetGameAsync();
    }

    public Task<SetupDto> GetSetup()
    {
        RequireAdmin();
        return setup.ReadAsync();
    }

    public Task<SetupDto> AddTeam(TeamInput input)
    {
        RequireAdmin();
        return setup.AddTeamAsync(input);
    }

    public Task<SetupDto> UpdateTeam(int teamId, TeamInput input)
    {
        RequireAdmin();
        return setup.UpdateTeamAsync(teamId, input);
    }

    public Task<SetupDto> DeleteTeam(int teamId)
    {
        RequireAdmin();
        return setup.DeleteTeamAsync(teamId);
    }

    public Task<SetupDto> AddQuestion(QuestionInput input)
    {
        RequireAdmin();
        return setup.AddQuestionAsync(input);
    }

    public Task<SetupDto> UpdateQuestion(int questionId, QuestionInput input)
    {
        RequireAdmin();
        return setup.UpdateQuestionAsync(questionId, input);
    }

    public Task<SetupDto> DeleteQuestion(int questionId)
    {
        RequireAdmin();
        return setup.DeleteQuestionAsync(questionId);
    }

    public Task<SetupDto> SetQuestionsPerGame(int count)
    {
        RequireAdmin();
        return setup.SetQuestionsPerGameAsync(count);
    }

    public Task<SetupDto> SetTeamsPerGame(int count)
    {
        RequireAdmin();
        return setup.SetTeamsPerGameAsync(count);
    }

    public Task<SetupDto> DrawQuestions()
    {
        RequireAdmin();
        return setup.DrawQuestionsAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await ReleasePresenceAsync();
        await base.OnDisconnectedAsync(exception);
    }

    private void RequireAdmin()
    {
        if (Context.Items[IsAdminKey] is not true)
            throw new GameRuleException("Admin only.");
    }

    private async Task<TeamSnapshot> AttachTeamAsync(int teamId)
    {
        await ReleasePresenceAsync();
        Context.Items[TeamIdKey] = teamId;
        await Groups.AddToGroupAsync(Context.ConnectionId, TeamsGroup);

        if (presence.Add(Context.ConnectionId, teamId))
            await notifier.TeamPresenceChangedAsync(teamId, true);

        return await game.GetTeamSnapshotAsync(teamId);
    }

    private async Task<AdminSnapshot> AttachAdminAsync()
    {
        Context.Items[IsAdminKey] = true;
        await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        return await game.GetAdminSnapshotAsync();
    }

    private async Task ReleasePresenceAsync()
    {
        if (presence.Remove(Context.ConnectionId) is { } offlineTeamId)
            await notifier.TeamPresenceChangedAsync(offlineTeamId, false);
    }
}
