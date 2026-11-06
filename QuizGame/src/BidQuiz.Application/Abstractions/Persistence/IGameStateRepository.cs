using BidQuiz.Domain.Games;

namespace BidQuiz.Application.Abstractions.Persistence;

/// <summary>The single game. It exists once the database has been seeded.</summary>
public interface IGameStateRepository
{
    /// <summary>The game, ready to change.</summary>
    Task<GameState> GetAsync(CancellationToken cancellationToken);

    /// <summary>The game, or null before the first seed.</summary>
    Task<GameState?> FindAsync(CancellationToken cancellationToken);

    void Add(GameState state);
}
