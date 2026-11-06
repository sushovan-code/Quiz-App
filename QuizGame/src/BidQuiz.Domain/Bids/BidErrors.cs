using BidQuiz.Domain.Common;
using BidQuiz.Domain.Games;

namespace BidQuiz.Domain.Bids;

public static class BidErrors
{
    public static readonly Error AnswerRequired = Error.Validation("bid.answer_required", "Please enter an answer.");

    public static readonly Error AnswerTooLong = Error.Validation(
        "bid.answer_too_long",
        $"Answers are limited to {GameLimits.MaxAnswerLength} characters.");

    public static readonly Error UnknownBid = Error.NotFound("bid.unknown", "Unknown bid.");

    public static readonly Error NotOpenForJudging =
        Error.Conflict("bid.not_open_for_judging", "That bid is not open for judging.");
}
