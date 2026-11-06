using BidQuiz.Api.Hubs;
using BidQuiz.Application.Abstractions.Realtime;
using BidQuiz.Application.Bids;
using BidQuiz.Application.Questions;
using BidQuiz.Application.Scores;
using Microsoft.AspNetCore.SignalR;

namespace BidQuiz.Api.Realtime;

/// <summary>Sends game events to SignalR groups. Bids and the correct answer only ever go to the host.</summary>
public sealed class SignalRGameNotifier(IHubContext<GameHub> hub) : IGameNotifier
{
    private IClientProxy Admin => hub.Clients.Group(GameHub.AdminGroup);

    private IClientProxy Everyone => hub.Clients.Groups(GameHub.AdminGroup, GameHub.TeamsGroup);

    public Task QuestionStartedAsync(
        QuestionResponse question,
        DateTime endsAtUtc,
        DateTime serverNowUtc,
        CancellationToken cancellationToken) =>
        Everyone.SendAsync(GameEvents.QuestionStarted, question, endsAtUtc, serverNowUtc, cancellationToken);

    public Task AnswerCountChangedAsync(int answered, int total, CancellationToken cancellationToken) =>
        Admin.SendAsync(GameEvents.AnswerCountChanged, answered, total, cancellationToken);

    public Task TimeUpAsync(CancellationToken cancellationToken) =>
        Everyone.SendAsync(GameEvents.TimeUp, cancellationToken);

    public Task BidsRevealedAsync(IReadOnlyList<BidResponse> bids, CancellationToken cancellationToken) =>
        Admin.SendAsync(GameEvents.BidsRevealed, bids, cancellationToken);

    public Task AnswerRevealedAsync(string text, CancellationToken cancellationToken) =>
        Admin.SendAsync(GameEvents.AnswerRevealed, text, cancellationToken);

    public Task ScoresUpdatedAsync(IReadOnlyList<TeamScoreResponse> teams, CancellationToken cancellationToken) =>
        Everyone.SendAsync(GameEvents.ScoresUpdated, teams, cancellationToken);

    public Task TeamPresenceChangedAsync(int teamId, bool online, CancellationToken cancellationToken) =>
        Everyone.SendAsync(GameEvents.TeamPresenceChanged, teamId, online, cancellationToken);

    public Task GameResetAsync(CancellationToken cancellationToken) =>
        Everyone.SendAsync(GameEvents.GameReset, cancellationToken);

    public Task QuestionBoardChangedAsync(
        IReadOnlyList<QuestionSummaryResponse> questions,
        CancellationToken cancellationToken) =>
        Admin.SendAsync(GameEvents.QuestionBoardChanged, questions, cancellationToken);

    // All clients, because a phone on the login screen hasn't joined a group yet.
    public Task RosterChangedAsync(CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync(GameEvents.RosterChanged, cancellationToken);
}
