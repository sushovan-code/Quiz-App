using BidQuiz.Application.Bids;
using BidQuiz.Application.Questions;
using BidQuiz.Application.Scores;
using BidQuiz.Domain.Games;

namespace BidQuiz.Application.Snapshots;

/// <summary>Everything the host screen needs to draw itself, after login or a reconnect.</summary>
public sealed record AdminSnapshotResponse(
    GameStatus Status,
    QuestionResponse? Question,
    DateTime? EndsAtUtc,
    DateTime ServerNowUtc,
    int AnsweredCount,
    int TeamCount,
    IReadOnlyList<QuestionSummaryResponse> Questions,
    IReadOnlyList<TeamScoreResponse> Teams,
    IReadOnlyList<BidResponse>? Bids,
    string? CorrectAnswer);
