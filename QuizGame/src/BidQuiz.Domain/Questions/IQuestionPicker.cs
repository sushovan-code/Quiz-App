namespace BidQuiz.Domain.Questions;

/// <summary>Chooses which questions from the bank go into a game (Strategy pattern).</summary>
public interface IQuestionPicker
{
    /// <summary>Returns up to <paramref name="count"/> question ids, in the order they will appear.</summary>
    IReadOnlyList<int> Pick(IReadOnlyList<QuestionCandidate> candidates, int count);
}
