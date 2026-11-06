namespace BidQuiz.Application.Scores;

/// <summary>One row of the leaderboard.</summary>
public sealed record TeamScoreResponse(
    int Id,
    string Name,
    string Color,
    int BidBalance,
    int Wallet,
    bool CanBid,
    bool Online);
