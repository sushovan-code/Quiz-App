namespace BidQuiz.Application.Setup;

/// <summary>Includes CorrectAnswer, so it only ever goes to the host.</summary>
public sealed record QuestionSetupResponse(
    int Id,
    int? Order,
    string Category,
    string Text,
    string CorrectAnswer,
    string? ImageUrl);
