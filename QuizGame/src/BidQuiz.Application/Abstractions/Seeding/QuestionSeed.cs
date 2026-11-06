namespace BidQuiz.Application.Abstractions.Seeding;

public sealed record QuestionSeed(string Category, string Text, string CorrectAnswer, string? ImageUrl);
