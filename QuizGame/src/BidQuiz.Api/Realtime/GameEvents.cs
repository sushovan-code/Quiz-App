namespace BidQuiz.Api.Realtime;

/// <summary>The names of the events the server pushes. The client lists the same names in ServerEvents.</summary>
public static class GameEvents
{
    public const string QuestionStarted = "QuestionStarted";
    public const string AnswerCountChanged = "AnswerCountChanged";
    public const string TimeUp = "TimeUp";
    public const string BidsRevealed = "BidsRevealed";
    public const string AnswerRevealed = "AnswerRevealed";
    public const string ScoresUpdated = "ScoresUpdated";
    public const string TeamPresenceChanged = "TeamPresenceChanged";
    public const string GameReset = "GameReset";
    public const string QuestionBoardChanged = "QuestionBoardChanged";
    public const string RosterChanged = "RosterChanged";
}
