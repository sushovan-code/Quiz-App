using BidQuiz.Domain.Scoring;

namespace BidQuiz.Application.Options;

/// <summary>Bound to the <c>Scoring</c> configuration section.</summary>
public sealed class ScoringOptions
{
    public const string SectionName = "Scoring";

    public int StartingBidBalance { get; set; } = 10000;

    public int StartingWallet { get; set; }

    public int BidStep { get; set; } = 1000;

    /// <summary>Percent of the bid added to the wallet for a correct highest bid.</summary>
    public int WinPercent { get; set; } = 1;

    /// <summary>Percent of the bid taken from the wallet for a wrong highest bid.</summary>
    public int LossPercent { get; set; } = 1;

    public ScoringRules ToRules() => new(StartingBidBalance, StartingWallet, BidStep, WinPercent, LossPercent);
}
