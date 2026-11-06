using Microsoft.Extensions.Options;

namespace BidQuiz.Server.Domain;

public readonly record struct Balances(int BidBalance, int Wallet);

/// <summary>The game's money rules. Swap the registration in Program.cs to change how a game scores.</summary>
public interface IScoringPolicy
{
    Balances Opening { get; }

    bool CanBid(int bidBalance);

    /// <summary>Returns the reason a bid is invalid, or null if it is acceptable.</summary>
    string? ValidateBid(int amount, int bidBalance);

    /// <summary>Returns a team's balances after a verdict on its bid. Never below zero.</summary>
    Balances Settle(Balances current, Verdict verdict, int bid, bool isHighest);
}

/// <summary>
/// Only the highest bid of a question moves money. Teams tied on the highest bid are each settled
/// on their own answer, so a correct team wins and a wrong team pays, whoever bid first.
/// </summary>
public sealed class HighestBidScoringPolicy(IOptions<ScoringOptions> options) : IScoringPolicy
{
    private readonly ScoringOptions _options = options.Value;

    public Balances Opening => new(_options.StartingBidBalance, _options.StartingWallet);

    public bool CanBid(int bidBalance) => bidBalance >= _options.BidStep;

    public string? ValidateBid(int amount, int bidBalance)
    {
        if (!CanBid(bidBalance))
            return "Your Bid Balance is 0, so you are out of the game.";
        if (amount <= 0 || amount % _options.BidStep != 0)
            return $"Bid must be a multiple of {_options.BidStep}.";
        if (amount > bidBalance)
            return "Bid exceeds your Bid Balance.";
        return null;
    }

    public Balances Settle(Balances current, Verdict verdict, int bid, bool isHighest)
    {
        if (!isHighest)
            return current;

        return verdict == Verdict.Up
            ? new Balances(current.BidBalance + bid, current.Wallet + Percent(bid, _options.WinPercent))
            : new Balances(
                Math.Max(0, current.BidBalance - bid),
                Math.Max(0, current.Wallet - Percent(bid, _options.LossPercent)));
    }

    private static int Percent(int bid, int percent) => bid * percent / 100;
}
