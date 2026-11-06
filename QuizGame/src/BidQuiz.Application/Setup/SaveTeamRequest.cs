namespace BidQuiz.Application.Setup;

/// <summary>A blank password on an existing team leaves its password unchanged.</summary>
public sealed record SaveTeamRequest(string Name, string? Password);
