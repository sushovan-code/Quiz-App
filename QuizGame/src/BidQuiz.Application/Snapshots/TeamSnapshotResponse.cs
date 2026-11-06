using BidQuiz.Application.Bids;
using BidQuiz.Application.Questions;
using BidQuiz.Application.Scores;
using BidQuiz.Domain.Games;

namespace BidQuiz.Application.Snapshots;

/// <summary>Everything a team device needs to draw its screen, after login or a reconnect.</summary>
public sealed record TeamSnapshotResponse(
    int TeamId,
    GameStatus Status,
    QuestionResponse? Question,
    DateTime? EndsAtUtc,
    DateTime ServerNowUtc,
    MyBidResponse? MyBid,
    IReadOnlyList<TeamScoreResponse> Teams);
