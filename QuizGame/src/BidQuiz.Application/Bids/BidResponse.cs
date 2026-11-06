using BidQuiz.Domain.Bids;
using BidQuiz.Domain.Teams;

namespace BidQuiz.Application.Bids;

/// <summary>A revealed bid. Host only.</summary>
public sealed record BidResponse(
    int Id,
    int TeamId,
    string TeamName,
    string TeamColor,
    string Answer,
    int Amount,
    Verdict? Verdict)
{
    public static BidResponse From(Bid bid, Team team) =>
        new(bid.Id, bid.TeamId, team.Name, team.Color, bid.Answer, bid.Amount, bid.Verdict);
}
