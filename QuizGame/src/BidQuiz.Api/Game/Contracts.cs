using BidQuiz.Api.Domain;

namespace BidQuiz.Api.Game;

/// <summary>Plays the game: questions, bids, the timer and judging.</summary>
public interface IGameService
{
    Task InitializeAsync();
    Task<TeamSnapshot> GetTeamSnapshotAsync(int teamId);
    Task<AdminSnapshot> GetAdminSnapshotAsync();
    Task StartQuestionAsync(int questionId);
    Task<MyBidDto> SubmitBidAsync(int teamId, string answer, int amount);
    Task TickAsync();
    Task RevealBidsAsync();
    Task RevealAnswerAsync();
    Task<BidDto> JudgeAsync(int bidId, Verdict verdict);
    Task ResetGameAsync();
}

/// <summary>Decides who is who: team and admin logins and their tokens.</summary>
public interface IAuthService
{
    Task<(int TeamId, string Token)> LoginTeamAsync(int teamId, string password);
    Task<int> ResolveTeamTokenAsync(string token);
    string LoginAdmin(string password);
    void ValidateAdminToken(string token);
}

/// <summary>Everything the host configures between games: teams, the question bank, the question count.</summary>
public interface ISetupService
{
    Task<IReadOnlyList<TeamOptionDto>> ListTeamsAsync();
    Task<SetupDto> ReadAsync();
    Task<SetupDto> AddTeamAsync(TeamInput input);
    Task<SetupDto> UpdateTeamAsync(int teamId, TeamInput input);
    Task<SetupDto> DeleteTeamAsync(int teamId);
    Task<SetupDto> AddQuestionAsync(QuestionInput input);
    Task<SetupDto> UpdateQuestionAsync(int questionId, QuestionInput input);
    Task<SetupDto> DeleteQuestionAsync(int questionId);
    Task<SetupDto> SetQuestionsPerGameAsync(int count);
    Task<SetupDto> SetTeamsPerGameAsync(int count);
    Task<SetupDto> DrawQuestionsAsync();
}

/// <summary>Which teams have a device connected.</summary>
public interface IPresence
{
    bool Add(string connectionId, int teamId);
    int? Remove(string connectionId);
    bool IsOnline(int teamId);
}
