namespace BidQuiz.Server.Game;

public interface IGameNotifier
{
    Task QuestionStartedAsync(QuestionDto question, DateTime endsAtUtc, DateTime serverNowUtc);
    Task AnswerCountChangedAsync(int answered, int total);
    Task TimeUpAsync();
    Task BidsRevealedAsync(IReadOnlyList<BidDto> bids);
    Task AnswerRevealedAsync(string text);
    Task ScoresUpdatedAsync(IReadOnlyList<TeamScoreDto> teams);
    Task TeamPresenceChangedAsync(int teamId, bool online);
    Task GameResetAsync();
    Task QuestionBoardChangedAsync(IReadOnlyList<QuestionSummaryDto> questions);
    Task RosterChangedAsync();
}
