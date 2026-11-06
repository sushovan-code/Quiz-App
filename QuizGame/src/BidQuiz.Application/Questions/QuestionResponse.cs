using BidQuiz.Domain.Questions;

namespace BidQuiz.Application.Questions;

/// <summary>Deliberately has no CorrectAnswer: this type is what team clients receive.</summary>
public sealed record QuestionResponse(int Id, int Order, string Category, string Text, string? ImageUrl)
{
    public static QuestionResponse From(Question question) =>
        new(question.Id, question.Order ?? 0, question.Category, question.Text, question.ImageUrl);
}
