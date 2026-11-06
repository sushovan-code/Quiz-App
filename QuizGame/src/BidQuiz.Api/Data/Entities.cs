using BidQuiz.Api.Domain;

namespace BidQuiz.Api.Data;

public class Team
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string PasswordHash { get; set; } = "";
    public required string Color { get; set; }
    public int BidBalance { get; set; }
    public int Wallet { get; set; }

    /// <summary>False for teams on the roster that sit this game out. See TeamRoster.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Lets a phone resume after a refresh or a server restart.</summary>
    public string? SessionToken { get; set; }
}

/// <summary>One entry in the question bank. Only the questions dealt into the game have an Order.</summary>
public class Question
{
    public int Id { get; set; }

    /// <summary>Position in the current game, or null while the question stays in the bank.</summary>
    public int? Order { get; set; }

    public required string Category { get; set; }
    public required string Text { get; set; }
    public required string CorrectAnswer { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>Set when the question is started, so it stays greyed out even if nobody answered.</summary>
    public bool Asked { get; set; }
}

public class Bid
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public Team? Team { get; set; }
    public int QuestionId { get; set; }
    public Question? Question { get; set; }
    public required string Answer { get; set; }
    public int Amount { get; set; }
    public DateTime SubmittedUtc { get; set; }
    public Verdict? Verdict { get; set; }

    /// <summary>
    /// What the verdict actually changed. Balances stop at zero, so the bid alone can't tell how
    /// much to give back when the host changes a verdict.
    /// </summary>
    public int BalanceDelta { get; set; }

    public int WalletDelta { get; set; }
}

public class GameState
{
    public int Id { get; set; }
    public int? CurrentQuestionId { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public GameStatus Status { get; set; }
    public bool AnswerRevealed { get; set; }
    public int QuestionsPerGame { get; set; } = GameLimits.MinQuestionsPerGame;
    public int TeamsPerGame { get; set; } = GameLimits.DefaultTeamsPerGame;
}
