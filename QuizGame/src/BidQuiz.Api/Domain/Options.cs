namespace BidQuiz.Api.Domain;

public static class GameLimits
{
    public const int MaxTeams = 20;
    public const int DefaultTeamsPerGame = 10;
    public const int MinQuestionsPerGame = 10;
    public const int MaxQuestionsPerGame = 25;
}

public sealed class ScoringOptions
{
    public int StartingBidBalance { get; set; } = 10000;
    public int StartingWallet { get; set; }
    public int BidStep { get; set; } = 1000;

    /// <summary>Percent of the bid added to the wallet for a correct highest bid.</summary>
    public int WinPercent { get; set; } = 1;

    /// <summary>Percent of the bid taken from the wallet for a wrong highest bid.</summary>
    public int LossPercent { get; set; } = 1;
}

public sealed class GameOptions
{
    public string AdminPassword { get; set; } = "";
    public string PublicBaseUrl { get; set; } = "";
    public int Port { get; set; } = 5000;
    public int QuestionSeconds { get; set; } = 30;

    /// <summary>Only the starting value: the host changes it on the setup screen.</summary>
    public int QuestionsPerGame { get; set; } = GameLimits.MinQuestionsPerGame;

    /// <summary>Only the starting value: the host changes it on the setup screen.</summary>
    public int TeamsPerGame { get; set; } = GameLimits.DefaultTeamsPerGame;
}
