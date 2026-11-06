using BidQuiz.Domain.Common;

namespace BidQuiz.Domain.Questions;

public static class QuestionErrors
{
    public static readonly Error UnknownQuestion = Error.NotFound("question.unknown", "Unknown question.");

    public static readonly Error NotInGame =
        Error.Conflict("question.not_in_game", "That question is not in this game.");

    public static readonly Error AlreadyAsked =
        Error.Conflict("question.already_asked", "That question has already been asked.");

    public static readonly Error ContentRequired = Error.Validation(
        "question.content_required",
        "A question needs a category, text and a correct answer.");

    public static readonly Error TooLong = Error.Validation("question.too_long", "That question is too long.");

    public static readonly Error Duplicate =
        Error.Conflict("question.duplicate", "That question is already in the bank.");
}
