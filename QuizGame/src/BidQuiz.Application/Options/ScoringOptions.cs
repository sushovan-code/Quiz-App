using System.ComponentModel.DataAnnotations;
using BidQuiz.Domain.Scoring;

namespace BidQuiz.Application.Options;

/// <summary>Bound to the <c>Scoring</c> configuration section.</summary>
public sealed class ScoringOptions
{
    public const string SectionName = "Scoring";

    public const int MaxPercent = 100;

    [Range(0, int.MaxValue)]
    public int StartingBidBalance { get; set; } = 10000;

    [Range(0, int.MaxValue)]
    public int StartingWallet { get; set; }

    [Range(1, int.MaxValue)]
    public int BidStep { get; set; } = 1000;

    /// <summary>Percent of the bid added to the wallet for a correct highest bid.</summary>
    [Range(0, MaxPercent)]
    public int WinPercent { get; set; } = 1;

    /// <summary>Percent of the bid taken from the wallet for a wrong highest bid.</summary>
    [Range(0, MaxPercent)]
    public int LossPercent { get; set; } = 1;

    public ScoringRules ToRules() => new(StartingBidBalance, StartingWallet, BidStep, WinPercent, LossPercent);
}
