using BidQuiz.Server.Game;
using Microsoft.AspNetCore.SignalR;

namespace BidQuiz.Server.Hubs;

public sealed class SignalRGameNotifier(IHubContext<GameHub> hub) : IGameNotifier
{
    private IClientProxy Admin => hub.Clients.Group(GameHub.AdminGroup);
    private IClientProxy Everyone => hub.Clients.Groups(GameHub.AdminGroup, GameHub.TeamsGroup);

    public Task QuestionStartedAsync(QuestionDto question, DateTime endsAtUtc, DateTime serverNowUtc) =>
        Everyone.SendAsync("QuestionStarted", question, endsAtUtc, serverNowUtc);

    public Task AnswerCountChangedAsync(int answered, int total) =>
        Admin.SendAsync("AnswerCountChanged", answered, total);

    public Task TimeUpAsync() => Everyone.SendAsync("TimeUp");

    // Bids and the correct answer never go to the teams group.
    public Task BidsRevealedAsync(IReadOnlyList<BidDto> bids) => Admin.SendAsync("BidsRevealed", bids);

    public Task AnswerRevealedAsync(string text) => Admin.SendAsync("AnswerRevealed", text);

    public Task ScoresUpdatedAsync(IReadOnlyList<TeamScoreDto> teams) => Everyone.SendAsync("ScoresUpdated", teams);

    public Task TeamPresenceChangedAsync(int teamId, bool online) =>
        Everyone.SendAsync("TeamPresenceChanged", teamId, online);

    public Task GameResetAsync() => Everyone.SendAsync("GameReset");

    public Task QuestionBoardChangedAsync(IReadOnlyList<QuestionSummaryDto> questions) =>
        Admin.SendAsync("QuestionBoardChanged", questions);

    // All clients, because a phone on the login screen hasn't joined a group yet.
    public Task RosterChangedAsync() => hub.Clients.All.SendAsync("RosterChanged");
}
