using BidQuiz.Domain.Bids;
using BidQuiz.Domain.Common;

namespace BidQuiz.Domain.Scoring;

/// <summary>The game's money rules (Strategy pattern). Register a different policy to change how a game scores.</summary>
public interface IScoringPolicy
{
    Balances Opening { get; }

    bool CanBid(int bidBalance);

    /// <summary>Checks a bid against the team's Bid Balance.</summary>
    Result ValidateBid(int amount, int bidBalance);

    /// <summary>Returns a team's balances after a verdict on its bid. Never below zero.</summary>
    Balances Settle(Balances current, Verdict verdict, int bid, bool isHighest);
}
