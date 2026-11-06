namespace BidQuiz.Application.Auth;

/// <summary>Who a team device is, and the token that lets it resume.</summary>
public sealed record TeamCredentials(int TeamId, string Token);
