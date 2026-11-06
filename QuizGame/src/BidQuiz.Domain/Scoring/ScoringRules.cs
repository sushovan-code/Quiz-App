namespace BidQuiz.Domain.Scoring;

/// <summary>The numbers a scoring policy works with.</summary>
/// <param name="StartingBidBalance">Bid Balance each team starts with.</param>
/// <param name="StartingWallet">Wallet each team starts with.</param>
/// <param name="BidStep">Bids must be multiples of this.</param>
/// <param name="WinPercent">Percent of the bid added to the wallet for a correct highest bid.</param>
/// <param name="LossPercent">Percent of the bid taken from the wallet for a wrong highest bid.</param>
public sealed record ScoringRules(
    int StartingBidBalance,
    int StartingWallet,
    int BidStep,
    int WinPercent,
    int LossPercent);
