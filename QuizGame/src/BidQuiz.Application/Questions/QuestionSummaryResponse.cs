namespace BidQuiz.Application.Questions;

/// <summary>One circle on the host's question board.</summary>
public sealed record QuestionSummaryResponse(int Id, int Order, bool Asked);
