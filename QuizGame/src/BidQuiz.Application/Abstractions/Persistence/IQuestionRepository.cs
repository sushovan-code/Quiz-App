using BidQuiz.Domain.Questions;

namespace BidQuiz.Application.Abstractions.Persistence;

public interface IQuestionRepository
{
    /// <summary>Finds a question to change. Null if there is no such question.</summary>
    Task<Question?> FindAsync(int questionId, CancellationToken cancellationToken);

    /// <summary>The whole bank, ready to change.</summary>
    Task<IReadOnlyList<Question>> ListAsync(CancellationToken cancellationToken);

    /// <summary>The questions dealt into the game, in game order. Read-only.</summary>
    Task<IReadOnlyList<Question>> ListBoardAsync(CancellationToken cancellationToken);

    /// <summary>The text of every question in the bank. Read-only.</summary>
    Task<IReadOnlyList<string>> ListTextsAsync(CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);

    Task<int> CountInGameAsync(CancellationToken cancellationToken);

    /// <summary>True once any question has been started. Setup is locked from then until a reset.</summary>
    Task<bool> AnyAskedAsync(CancellationToken cancellationToken);

    /// <summary>Whether another question already has this text, ignoring case.</summary>
    Task<bool> ExistsWithTextAsync(string text, int? exceptQuestionId, CancellationToken cancellationToken);

    void Add(Question question);

    void Remove(Question question);
}
