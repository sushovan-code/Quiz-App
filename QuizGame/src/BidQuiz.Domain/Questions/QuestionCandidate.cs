namespace BidQuiz.Domain.Questions;

/// <summary>What a picker needs to know about a question in the bank.</summary>
public sealed record QuestionCandidate(int Id, string Answer);
