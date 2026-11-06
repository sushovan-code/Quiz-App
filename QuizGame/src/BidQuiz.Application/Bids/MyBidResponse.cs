namespace BidQuiz.Application.Bids;

/// <summary>What a team sees of its own bid.</summary>
public sealed record MyBidResponse(string Answer, int Amount);
