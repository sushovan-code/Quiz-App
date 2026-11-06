namespace BidQuiz.Api.Endpoints;

/// <summary>What the browser needs before it connects: where phones join, the bid step and the question time.</summary>
public sealed record ClientConfigResponse(string JoinUrl, int BidStep, int QuestionSeconds);
