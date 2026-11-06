using BidQuiz.Application.Abstractions.Presence;
using BidQuiz.Application.Abstractions.Realtime;
using BidQuiz.Application.Auth;
using BidQuiz.Application.Auth.AdminLogin;
using BidQuiz.Application.Auth.AdminLogout;
using BidQuiz.Application.Auth.JoinTeam;
using BidQuiz.Application.Auth.ResumeAdmin;
using BidQuiz.Application.Auth.ResumeTeam;
using BidQuiz.Application.Bids;
using BidQuiz.Application.Bids.JudgeBid;
using BidQuiz.Application.Bids.SubmitBid;
using BidQuiz.Application.Common;
using BidQuiz.Application.Game.ResetGame;
using BidQuiz.Application.Game.RevealAnswer;
using BidQuiz.Application.Game.RevealBids;
using BidQuiz.Application.Game.StartQuestion;
using BidQuiz.Application.Setup;
using BidQuiz.Application.Setup.AddQuestion;
using BidQuiz.Application.Setup.AddTeam;
using BidQuiz.Application.Setup.DeleteQuestion;
using BidQuiz.Application.Setup.DeleteTeam;
using BidQuiz.Application.Setup.DrawQuestions;
using BidQuiz.Application.Setup.GetSetup;
using BidQuiz.Application.Setup.SetQuestionsPerGame;
using BidQuiz.Application.Setup.SetTeamsPerGame;
using BidQuiz.Application.Setup.UpdateQuestion;
using BidQuiz.Application.Setup.UpdateTeam;
using BidQuiz.Application.Snapshots;
using BidQuiz.Application.Snapshots.GetAdminSnapshot;
using BidQuiz.Application.Snapshots.GetTeamSnapshot;
using BidQuiz.Domain.Bids;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BidQuiz.Api.Hubs;

/// <summary>
/// The game's realtime API. It decides who is calling, then hands the call to a use case.
/// Method names are the contract with the client, so renaming one is a breaking change.
/// Use cases are injected per method so the hub stays thin.
/// </summary>
public sealed class GameHub(
    ITeamPresence presence,
    IGameNotifier notifier,
    GetTeamSnapshotHandler teamSnapshots,
    GetAdminSnapshotHandler adminSnapshots) : Hub
{
    public const string Path = "/hubs/game";
    public const string AdminGroup = "admin";
    public const string TeamsGroup = "teams";

    private const string TeamIdKey = "teamId";
    private const string IsAdminKey = "isAdmin";

    private CancellationToken Aborted => Context.ConnectionAborted;

    /// <summary>Logs this device in as a team.</summary>
    public async Task<TeamSessionResponse> JoinTeam(int teamId, string password, [FromServices] JoinTeamHandler handler)
    {
        var credentials = (await handler.HandleAsync(new JoinTeam(teamId, password), Aborted)).ValueOrThrow();
        return new TeamSessionResponse(credentials.Token, await AttachTeamAsync(credentials.TeamId));
    }

    /// <summary>Resumes a team session after a refresh or reconnect, with no password.</summary>
    public async Task<TeamSessionResponse> ResumeTeam(string token, [FromServices] ResumeTeamHandler handler)
    {
        var teamId = (await handler.HandleAsync(new ResumeTeam(token), Aborted)).ValueOrThrow();
        return new TeamSessionResponse(token, await AttachTeamAsync(teamId));
    }

    /// <summary>Logs this device out. The team stays online if another of its devices is connected.</summary>
    public async Task LeaveTeam()
    {
        await ReleasePresenceAsync();
        Context.Items.Remove(TeamIdKey);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, TeamsGroup, Aborted);
    }

    public async Task<AdminSessionResponse> AdminLogin(string password, [FromServices] AdminLoginHandler handler)
    {
        var token = handler.Handle(new AdminLogin(password)).ValueOrThrow();
        return new AdminSessionResponse(token, await AttachAdminAsync());
    }

    public async Task<AdminSessionResponse> ResumeAdmin(string token, [FromServices] ResumeAdminHandler handler)
    {
        handler.Handle(new ResumeAdmin(token)).ThrowIfFailed();
        return new AdminSessionResponse(token, await AttachAdminAsync());
    }

    /// <summary>
    /// Logs the host out: revokes the token and stops treating this connection as admin.
    /// The game is untouched. Anyone may call it, as it only affects the caller and the token they hold.
    /// </summary>
    public async Task AdminLogout(string token, [FromServices] AdminLogoutHandler handler)
    {
        handler.Handle(new AdminLogout(token)).ThrowIfFailed();
        Context.Items.Remove(IsAdminKey);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, AdminGroup, Aborted);
    }

    public async Task StartQuestion(int questionId, [FromServices] StartQuestionHandler handler)
    {
        RequireAdmin();
        (await handler.HandleAsync(new StartQuestion(questionId), Aborted)).ThrowIfFailed();
    }

    public async Task<MyBidResponse> SubmitBid(string answer, int amount, [FromServices] SubmitBidHandler handler)
    {
        if (Context.Items[TeamIdKey] is not int teamId)
        {
            throw AuthErrors.TeamLoginRequired.ToHubException();
        }

        return (await handler.HandleAsync(new SubmitBid(teamId, answer, amount), Aborted)).ValueOrThrow();
    }

    public async Task RevealBids([FromServices] RevealBidsHandler handler)
    {
        RequireAdmin();
        (await handler.HandleAsync(Aborted)).ThrowIfFailed();
    }

    public async Task RevealAnswer([FromServices] RevealAnswerHandler handler)
    {
        RequireAdmin();
        (await handler.HandleAsync(Aborted)).ThrowIfFailed();
    }

    public async Task<BidResponse> Judge(int bidId, Verdict verdict, [FromServices] JudgeBidHandler handler)
    {
        RequireAdmin();
        return (await handler.HandleAsync(new JudgeBid(bidId, verdict), Aborted)).ValueOrThrow();
    }

    public Task ResetGame([FromServices] ResetGameHandler handler)
    {
        RequireAdmin();
        return handler.HandleAsync(Aborted);
    }

    public Task<SetupResponse> GetSetup([FromServices] GetSetupHandler handler)
    {
        RequireAdmin();
        return handler.HandleAsync(Aborted);
    }

    public async Task<SetupResponse> AddTeam(SaveTeamRequest input, [FromServices] AddTeamHandler handler)
    {
        RequireAdmin();
        return (await handler.HandleAsync(new AddTeam(input), Aborted)).ValueOrThrow();
    }

    public async Task<SetupResponse> UpdateTeam(int teamId, SaveTeamRequest input, [FromServices] UpdateTeamHandler handler)
    {
        RequireAdmin();
        return (await handler.HandleAsync(new UpdateTeam(teamId, input), Aborted)).ValueOrThrow();
    }

    public async Task<SetupResponse> DeleteTeam(int teamId, [FromServices] DeleteTeamHandler handler)
    {
        RequireAdmin();
        return (await handler.HandleAsync(new DeleteTeam(teamId), Aborted)).ValueOrThrow();
    }

    public async Task<SetupResponse> AddQuestion(SaveQuestionRequest input, [FromServices] AddQuestionHandler handler)
    {
        RequireAdmin();
        return (await handler.HandleAsync(new AddQuestion(input), Aborted)).ValueOrThrow();
    }

    public async Task<SetupResponse> UpdateQuestion(
        int questionId,
        SaveQuestionRequest input,
        [FromServices] UpdateQuestionHandler handler)
    {
        RequireAdmin();
        return (await handler.HandleAsync(new UpdateQuestion(questionId, input), Aborted)).ValueOrThrow();
    }

    public async Task<SetupResponse> DeleteQuestion(int questionId, [FromServices] DeleteQuestionHandler handler)
    {
        RequireAdmin();
        return (await handler.HandleAsync(new DeleteQuestion(questionId), Aborted)).ValueOrThrow();
    }

    public async Task<SetupResponse> SetQuestionsPerGame(int count, [FromServices] SetQuestionsPerGameHandler handler)
    {
        RequireAdmin();
        return (await handler.HandleAsync(new SetQuestionsPerGame(count), Aborted)).ValueOrThrow();
    }

    public async Task<SetupResponse> SetTeamsPerGame(int count, [FromServices] SetTeamsPerGameHandler handler)
    {
        RequireAdmin();
        return (await handler.HandleAsync(new SetTeamsPerGame(count), Aborted)).ValueOrThrow();
    }

    public async Task<SetupResponse> DrawQuestions([FromServices] DrawQuestionsHandler handler)
    {
        RequireAdmin();
        return (await handler.HandleAsync(Aborted)).ValueOrThrow();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await ReleasePresenceAsync();
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>The admin flag is set on the connection at login, so a client can't claim it.</summary>
    private void RequireAdmin()
    {
        if (Context.Items[IsAdminKey] is not true)
        {
            throw AuthErrors.AdminOnly.ToHubException();
        }
    }

    private async Task<TeamSnapshotResponse> AttachTeamAsync(int teamId)
    {
        await ReleasePresenceAsync();
        Context.Items[TeamIdKey] = teamId;
        await Groups.AddToGroupAsync(Context.ConnectionId, TeamsGroup, Aborted);

        if (presence.Add(Context.ConnectionId, teamId))
        {
            await notifier.TeamPresenceChangedAsync(teamId, true, AfterCommit.Token);
        }

        return await teamSnapshots.HandleAsync(new GetTeamSnapshot(teamId), Aborted);
    }

    private async Task<AdminSnapshotResponse> AttachAdminAsync()
    {
        Context.Items[IsAdminKey] = true;
        await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup, Aborted);
        return await adminSnapshots.HandleAsync(Aborted);
    }

    private async Task ReleasePresenceAsync()
    {
        if (presence.Remove(Context.ConnectionId) is { } offlineTeamId)
        {
            await notifier.TeamPresenceChangedAsync(offlineTeamId, false, AfterCommit.Token);
        }
    }
}
