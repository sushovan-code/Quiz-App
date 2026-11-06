using System.ComponentModel.DataAnnotations;
using BidQuiz.Domain.Games;

namespace BidQuiz.Application.Options;

/// <summary>Bound to the <c>Game</c> configuration section.</summary>
public sealed class GameOptions
{
    public const string SectionName = "Game";

    public const int MaxQuestionSeconds = 600;

    /// <summary>The host's password. Comes from user secrets locally and app settings in the cloud.</summary>
    [Required(ErrorMessage = "Game:AdminPassword is not set. Locally run: dotnet user-secrets set \"Game:AdminPassword\" \"<password>\" --project src/BidQuiz.Api")]
    public string AdminPassword { get; set; } = "";

    /// <summary>Time allowed per question.</summary>
    [Range(1, MaxQuestionSeconds)]
    public int QuestionSeconds { get; set; } = 30;

    /// <summary>Only the starting value: the host changes it on the setup screen.</summary>
    public int QuestionsPerGame { get; set; } = GameLimits.MinQuestionsPerGame;

    /// <summary>Only the starting value: the host changes it on the setup screen.</summary>
    public int TeamsPerGame { get; set; } = GameLimits.DefaultTeamsPerGame;
}
