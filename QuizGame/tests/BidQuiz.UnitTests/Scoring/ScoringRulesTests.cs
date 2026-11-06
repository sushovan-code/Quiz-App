using BidQuiz.Application.Options;
using BidQuiz.Domain.Bids;
using BidQuiz.Domain.Scoring;

namespace BidQuiz.UnitTests.Scoring;

public class ScoringRulesTests
{
    private static readonly HighestBidScoringPolicy Policy = new(new ScoringOptions().ToRules());

    [Fact]
    public void TeamsOpen_WithTenThousandAndAnEmptyWallet() =>
        Assert.Equal(new Balances(10000, 0), Policy.Opening);

    [Fact]
    public void CorrectHighestBid_AddsBidToBalanceAndOnePercentToWallet() =>
        Assert.Equal(new Balances(13000, 130), Policy.Settle(new(10000, 100), Verdict.Up, 3000, isHighest: true));

    [Fact]
    public void WrongHighestBid_TakesBidFromBalanceAndOnePercentFromWallet() =>
        Assert.Equal(new Balances(7000, 70), Policy.Settle(new(10000, 100), Verdict.Down, 3000, isHighest: true));

    [Theory]
    [InlineData(Verdict.Up)]
    [InlineData(Verdict.Down)]
    public void LowerBids_MoveNoMoney(Verdict verdict) =>
        Assert.Equal(new Balances(10000, 100), Policy.Settle(new(10000, 100), verdict, 3000, isHighest: false));

    [Fact]
    public void WalletStopsAtZero() =>
        Assert.Equal(new Balances(7000, 0), Policy.Settle(new(10000, 10), Verdict.Down, 3000, isHighest: true));

    [Fact]
    public void BidBalanceStopsAtZero() =>
        Assert.Equal(new Balances(0, 0), Policy.Settle(new(3000, 0), Verdict.Down, 5000, isHighest: true));

    [Fact]
    public void Percentages_AreConfigurable()
    {
        var policy = new HighestBidScoringPolicy(new ScoringOptions { WinPercent = 5, LossPercent = 2 }.ToRules());

        Assert.Equal(50, policy.Settle(default, Verdict.Up, 1000, isHighest: true).Wallet);
        Assert.Equal(80, policy.Settle(new(1000, 100), Verdict.Down, 1000, isHighest: true).Wallet);
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(25000)]
    public void ValidateBid_AcceptsMultiplesOfTheStep(int amount) =>
        Assert.True(Policy.ValidateBid(amount, 50000).IsSuccess);

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    [InlineData(999)]
    [InlineData(1500)]
    public void ValidateBid_RejectsAnythingElse(int amount) =>
        Assert.True(Policy.ValidateBid(amount, 50000).IsFailure);

    [Fact]
    public void ValidateBid_RejectsMoreThanBalance() =>
        Assert.Equal("Bid exceeds your Bid Balance.", Policy.ValidateBid(4000, 3000).Error?.Message);

    [Fact]
    public void ValidateBid_AllowsWholeBalance() =>
        Assert.True(Policy.ValidateBid(3000, 3000).IsSuccess);

    [Fact]
    public void ZeroBalance_CannotBid()
    {
        Assert.False(Policy.CanBid(0));
        Assert.Contains("out of the game", Policy.ValidateBid(1000, 0).Error?.Message, StringComparison.Ordinal);
    }
}
