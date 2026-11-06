using BidQuiz.Domain.Common;
using BidQuiz.Domain.Games;

namespace BidQuiz.Domain.Bids;

/// <summary>The rules for what a team types as its answer.</summary>
public static class BidAnswer
{
    public static Result<string> Normalize(string answer)
    {
        var trimmed = answer.Trim();
        if (trimmed.Length == 0)
        {
            return BidErrors.AnswerRequired;
        }

        return trimmed.Length > GameLimits.MaxAnswerLength ? BidErrors.AnswerTooLong : trimmed;
    }
}
