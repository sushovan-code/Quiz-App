using BidQuiz.Domain.Bids;
using BidQuiz.Domain.Common;

namespace BidQuiz.Domain.Scoring;

/// <summary>
/// Only the highest bid of a question moves money. Teams tied on the highest bid are each settled
/// on their own answer, so a correct team wins and a wrong team pays, whoever bid first.
/// </summary>
public sealed class HighestBidScoringPolicy(ScoringRules rules) : IScoringPolicy
{
    private const int PercentDivisor = 100;

    public Balances Opening => new(rules.StartingBidBalance, rules.StartingWallet);

    public bool CanBid(int bidBalance) => bidBalance >= rules.BidStep;

    public Result ValidateBid(int amount, int bidBalance)
    {
        if (!CanBid(bidBalance))
        {
            return ScoringErrors.OutOfTheGame;
        }

        if (amount <= 0 || amount % rules.BidStep != 0)
        {
            return ScoringErrors.NotAMultipleOfStep(rules.BidStep);
        }

        return amount > bidBalance ? ScoringErrors.OverBalance : Result.Success();
    }

    public Balances Settle(Balances current, Verdict verdict, int bid, bool isHighest)
    {
        if (!isHighest)
        {
            return current;
        }

        return verdict == Verdict.Up
            ? new Balances(current.BidBalance + bid, current.Wallet + Percent(bid, rules.WinPercent))
            : new Balances(
                Math.Max(0, current.BidBalance - bid),
                Math.Max(0, current.Wallet - Percent(bid, rules.LossPercent)));
    }

    private static int Percent(int bid, int percent) => bid * percent / PercentDivisor;
}
