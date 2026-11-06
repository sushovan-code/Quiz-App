using BidQuiz.Domain.Bids;

namespace BidQuiz.Application.Bids.JudgeBid;

public sealed record JudgeBid(int BidId, Verdict Verdict);
