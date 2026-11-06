using BidQuiz.Application.Abstractions.Realtime;
using BidQuiz.Application.Bids;
using BidQuiz.Application.Questions;
using BidQuiz.Application.Scores;

namespace BidQuiz.IntegrationTests.Game;

/// <summary>Keeps the last value of each event so tests can check what clients were told.</summary>
internal sealed class RecordingNotifier : IGameNotifier
{
    public int TimeUpCount { get; private set; }

    public int ResetCount { get; private set; }

    public int LastAnswered { get; private set; }

    public IReadOnlyList<BidResponse> RevealedBids { get; private set; } = [];

    public IReadOnlyList<TeamScoreResponse> LastScores { get; private set; } = [];

    public IReadOnlyList<QuestionSummaryResponse> LastBoard { get; private set; } = [];

    public Task QuestionStartedAsync(
        QuestionResponse question,
        DateTime endsAtUtc,
        DateTime serverNowUtc,
        CancellationToken cancellationToken) => Task.CompletedTask;

    public Task AnswerCountChangedAsync(int answered, int total, CancellationToken cancellationToken)
    {
        LastAnswered = answered;
        return Task.CompletedTask;
    }

    public Task TimeUpAsync(CancellationToken cancellationToken)
    {
        TimeUpCount++;
        return Task.CompletedTask;
    }

    public Task BidsRevealedAsync(IReadOnlyList<BidResponse> bids, CancellationToken cancellationToken)
    {
        RevealedBids = bids;
        return Task.CompletedTask;
    }

    public Task AnswerRevealedAsync(string text, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ScoresUpdatedAsync(IReadOnlyList<TeamScoreResponse> teams, CancellationToken cancellationToken)
    {
        LastScores = teams;
        return Task.CompletedTask;
    }

    public Task TeamPresenceChangedAsync(int teamId, bool online, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task GameResetAsync(CancellationToken cancellationToken)
    {
        ResetCount++;
        return Task.CompletedTask;
    }

    public Task QuestionBoardChangedAsync(
        IReadOnlyList<QuestionSummaryResponse> questions,
        CancellationToken cancellationToken)
    {
        LastBoard = questions;
        return Task.CompletedTask;
    }

    public Task RosterChangedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
