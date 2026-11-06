using BidQuiz.Domain.Common;

namespace BidQuiz.Domain.Scoring;

public static class ScoringErrors
{
    public static readonly Error OutOfTheGame =
        Error.Validation("bid.out_of_the_game", "Your Bid Balance is 0, so you are out of the game.");

    public static readonly Error OverBalance =
        Error.Validation("bid.over_balance", "Bid exceeds your Bid Balance.");

    public static Error NotAMultipleOfStep(int bidStep) =>
        Error.Validation("bid.step", $"Bid must be a multiple of {bidStep}.");
}
