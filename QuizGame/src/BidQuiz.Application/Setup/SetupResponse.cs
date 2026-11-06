namespace BidQuiz.Application.Setup;

/// <summary>Everything the setup screen shows. Host only.</summary>
public sealed record SetupResponse(
    IReadOnlyList<TeamSetupResponse> Teams,
    IReadOnlyList<QuestionSetupResponse> Questions,
    int QuestionsPerGame,
    int TeamsPerGame,
    int MaxTeams,
    int MinQuestionsPerGame,
    int MaxQuestionsPerGame,
    bool Locked);
