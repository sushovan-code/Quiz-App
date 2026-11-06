using BidQuiz.Domain.Games;

namespace BidQuiz.Application.Options;

/// <summary>Bound to the <c>Game</c> configuration section.</summary>
public sealed class GameOptions
{
    public const string SectionName = "Game";

    /// <summary>The host's password. Comes from user secrets locally and app settings in the cloud.</summary>
    public string AdminPassword { get; set; } = "";

    /// <summary>Time allowed per question.</summary>
    public int QuestionSeconds { get; set; } = 30;

    /// <summary>Only the starting value: the host changes it on the setup screen.</summary>
    public int QuestionsPerGame { get; set; } = GameLimits.MinQuestionsPerGame;

    /// <summary>Only the starting value: the host changes it on the setup screen.</summary>
    public int TeamsPerGame { get; set; } = GameLimits.DefaultTeamsPerGame;
}
