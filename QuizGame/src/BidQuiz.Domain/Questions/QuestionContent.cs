using BidQuiz.Domain.Common;
using BidQuiz.Domain.Games;

namespace BidQuiz.Domain.Questions;

/// <summary>What the host writes for a question.</summary>
public sealed record QuestionContent(string Category, string Text, string CorrectAnswer, string? ImageUrl)
{
    /// <summary>Trims the host's input and checks it is complete and not too long.</summary>
    public static Result<QuestionContent> Create(string category, string text, string correctAnswer, string? imageUrl)
    {
        var content = new QuestionContent(
            category.Trim(),
            text.Trim(),
            correctAnswer.Trim(),
            string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim());

        if (content.Text.Length == 0 || content.CorrectAnswer.Length == 0 || content.Category.Length == 0)
        {
            return QuestionErrors.ContentRequired;
        }

        var isTooLong = content.Text.Length > GameLimits.MaxTextLength
            || content.CorrectAnswer.Length > GameLimits.MaxTextLength
            || content.Category.Length > GameLimits.MaxNameLength;
        return isTooLong ? QuestionErrors.TooLong : content;
    }
}
