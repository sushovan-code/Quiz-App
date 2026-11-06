namespace BidQuiz.Server.Domain;

/// <summary>A rule was broken. The message is safe to show to the player or host.</summary>
public sealed class GameRuleException(string message) : Exception(message);
