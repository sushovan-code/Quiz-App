using BidQuiz.Domain.Bids;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Questions;

namespace BidQuiz.Domain.Games;

/// <summary>
/// The single game in progress. Everything about the running question lives here, so a server
/// restart mid-question resumes where it left off.
/// </summary>
public sealed class GameState
{
    private GameState()
    {
    }

    public int Id { get; private set; }

    public int? CurrentQuestionId { get; private set; }

    public DateTime? EndsAtUtc { get; private set; }

    public GameStatus Status { get; private set; }

    public bool IsAnswerRevealed { get; private set; }

    public int QuestionsPerGame { get; private set; } = GameLimits.MinQuestionsPerGame;

    public int TeamsPerGame { get; private set; } = GameLimits.DefaultTeamsPerGame;

    /// <summary>The deadline of the question that is taking bids, if any.</summary>
    public DateTime? OpenDeadline => Status == GameStatus.Accepting ? EndsAtUtc : null;

    public bool IsAccepting => Status == GameStatus.Accepting;

    public bool AreBidsRevealed => Status == GameStatus.Revealed;

    public static GameState Create(int questionsPerGame, int teamsPerGame) => new()
    {
        Status = GameStatus.Idle,
        QuestionsPerGame = questionsPerGame,
        TeamsPerGame = teamsPerGame
    };

    /// <summary>Opens <paramref name="question"/> for bids until <paramref name="endsAtUtc"/>.</summary>
    public Result StartQuestion(Question? question, DateTime endsAtUtc)
    {
        if (IsAccepting)
        {
            return GameErrors.QuestionRunning;
        }

        if (question is null)
        {
            return QuestionErrors.UnknownQuestion;
        }

        if (!question.IsInGame)
        {
            return QuestionErrors.NotInGame;
        }

        if (question.IsAsked)
        {
            return QuestionErrors.AlreadyAsked;
        }

        question.MarkAsked();
        CurrentQuestionId = question.Id;
        EndsAtUtc = endsAtUtc;
        Status = GameStatus.Accepting;
        IsAnswerRevealed = false;
        return Result.Success();
    }

    /// <summary>Checks the clock rather than the status: the deadline tick may not have fired yet.</summary>
    public Result AcceptsBidsAt(DateTime nowUtc) =>
        IsAccepting && EndsAtUtc is { } endsAt && nowUtc < endsAt ? Result.Success() : GameErrors.TimeUp;

    /// <summary>Stops taking bids. Returns false if no question was open.</summary>
    public bool Close()
    {
        if (!IsAccepting)
        {
            return false;
        }

        Status = GameStatus.Closed;
        return true;
    }

    /// <summary>Shows the bids to the host. The value says whether bidding was still open.</summary>
    public Result<bool> RevealBids()
    {
        if (Status == GameStatus.Idle)
        {
            return GameErrors.NothingAsked;
        }

        var closedEarly = IsAccepting;
        Status = GameStatus.Revealed;
        return closedEarly;
    }

    public Result RevealAnswer()
    {
        if (!AreBidsRevealed)
        {
            return GameErrors.RevealBidsFirst;
        }

        IsAnswerRevealed = true;
        return Result.Success();
    }

    public bool IsOpenForJudging(Bid bid) => AreBidsRevealed && bid.QuestionId == CurrentQuestionId;

    /// <summary>Back to the start, ready for a new game.</summary>
    public void Reset()
    {
        CurrentQuestionId = null;
        EndsAtUtc = null;
        Status = GameStatus.Idle;
        IsAnswerRevealed = false;
    }

    public void SetQuestionsPerGame(int count) => QuestionsPerGame = count;

    public void SetTeamsPerGame(int count) => TeamsPerGame = count;
}
