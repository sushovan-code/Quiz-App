namespace BidQuiz.Application.Setup;

public sealed record SaveQuestionRequest(string Category, string Text, string CorrectAnswer, string? ImageUrl);
