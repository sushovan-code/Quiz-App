namespace BidQuiz.Domain.Games;

/// <summary>Fixed limits of the show.</summary>
public static class GameLimits
{
    public const int MaxTeams = 20;
    public const int DefaultTeamsPerGame = 10;
    public const int MinQuestionsPerGame = 10;
    public const int MaxQuestionsPerGame = 25;
    public const int MaxAnswerLength = 500;
    public const int MaxNameLength = 40;
    public const int MaxTextLength = 500;

    public static int ClampQuestionsPerGame(int count) =>
        Math.Clamp(count, MinQuestionsPerGame, MaxQuestionsPerGame);

    public static int ClampTeamsPerGame(int count) => Math.Clamp(count, 1, MaxTeams);
}
