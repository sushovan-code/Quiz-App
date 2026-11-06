using BidQuiz.Server.Domain;

namespace BidQuiz.Server.Game;

/// <summary>Deliberately has no CorrectAnswer: this type is what team clients receive.</summary>
public sealed record QuestionDto(int Id, int Order, string Category, string Text, string? ImageUrl);

public sealed record QuestionSummaryDto(int Id, int Order, bool Asked);

public sealed record TeamScoreDto(
    int Id,
    string Name,
    string Color,
    int BidBalance,
    int Wallet,
    bool CanBid,
    bool Online);

public sealed record TeamOptionDto(int Id, string Name);

public sealed record BidDto(
    int Id,
    int TeamId,
    string TeamName,
    string TeamColor,
    string Answer,
    int Amount,
    Verdict? Verdict);

public sealed record MyBidDto(string Answer, int Amount);

public sealed record TeamSnapshot(
    int TeamId,
    GameStatus Status,
    QuestionDto? Question,
    DateTime? EndsAtUtc,
    DateTime ServerNowUtc,
    MyBidDto? MyBid,
    IReadOnlyList<TeamScoreDto> Teams);

public sealed record AdminSnapshot(
    GameStatus Status,
    QuestionDto? Question,
    DateTime? EndsAtUtc,
    DateTime ServerNowUtc,
    int AnsweredCount,
    int TeamCount,
    IReadOnlyList<QuestionSummaryDto> Questions,
    IReadOnlyList<TeamScoreDto> Teams,
    IReadOnlyList<BidDto>? Bids,
    string? CorrectAnswer);

public sealed record TeamSession(string Token, TeamSnapshot Snapshot);

public sealed record AdminSession(string Token, AdminSnapshot Snapshot);

public sealed record ClientConfig(string JoinUrl, int BidStep, int QuestionSeconds);

/// <summary>A blank password on an existing team leaves its password unchanged.</summary>
public sealed record TeamInput(string Name, string? Password);

public sealed record QuestionInput(string Category, string Text, string CorrectAnswer, string? ImageUrl);

public sealed record TeamSetupDto(int Id, string Name, string Color, bool Playing);

/// <summary>Includes CorrectAnswer, so it only ever goes to an admin.</summary>
public sealed record QuestionSetupDto(
    int Id,
    int? Order,
    string Category,
    string Text,
    string CorrectAnswer,
    string? ImageUrl);

public sealed record SetupDto(
    IReadOnlyList<TeamSetupDto> Teams,
    IReadOnlyList<QuestionSetupDto> Questions,
    int QuestionsPerGame,
    int TeamsPerGame,
    int MaxTeams,
    int MinQuestionsPerGame,
    int MaxQuestionsPerGame,
    bool Locked);
