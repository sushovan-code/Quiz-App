using BidQuiz.Application.Bids;
using BidQuiz.Application.Questions;
using BidQuiz.Application.Scores;

namespace BidQuiz.Application.Abstractions.Realtime;

/// <summary>Tells connected clients what changed. Use cases never reference the realtime transport.</summary>
public interface IGameNotifier
{
    Task QuestionStartedAsync(
        QuestionResponse question,
        DateTime endsAtUtc,
        DateTime serverNowUtc,
        CancellationToken cancellationToken);

    /// <summary>Host only.</summary>
    Task AnswerCountChangedAsync(int answered, int total, CancellationToken cancellationToken);

    Task TimeUpAsync(CancellationToken cancellationToken);

    /// <summary>Host only: bids never go to teams.</summary>
    Task BidsRevealedAsync(IReadOnlyList<BidResponse> bids, CancellationToken cancellationToken);

    /// <summary>Host only: the correct answer never goes to teams.</summary>
    Task AnswerRevealedAsync(string text, CancellationToken cancellationToken);

    Task ScoresUpdatedAsync(IReadOnlyList<TeamScoreResponse> teams, CancellationToken cancellationToken);

    Task TeamPresenceChangedAsync(int teamId, bool online, CancellationToken cancellationToken);

    Task GameResetAsync(CancellationToken cancellationToken);

    /// <summary>Host only.</summary>
    Task QuestionBoardChangedAsync(IReadOnlyList<QuestionSummaryResponse> questions, CancellationToken cancellationToken);

    /// <summary>Everyone, including devices still on the login screen.</summary>
    Task RosterChangedAsync(CancellationToken cancellationToken);
}
