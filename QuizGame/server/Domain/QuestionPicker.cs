namespace BidQuiz.Server.Domain;

public sealed record QuestionCandidate(int Id, string Answer);

public interface IQuestionPicker
{
    /// <summary>Returns up to <paramref name="count"/> question ids, in the order they will appear.</summary>
    IReadOnlyList<int> Pick(IReadOnlyList<QuestionCandidate> candidates, int count);
}

public sealed class RandomQuestionPicker : IQuestionPicker
{
    /// <summary>Two questions with the same movie as their answer never appear in one game.</summary>
    public IReadOnlyList<int> Pick(IReadOnlyList<QuestionCandidate> candidates, int count)
    {
        var shuffled = candidates.ToArray();
        Random.Shared.Shuffle(shuffled);
        return shuffled
            .DistinctBy(c => c.Answer.Trim().ToUpperInvariant())
            .Take(count)
            .Select(c => c.Id)
            .ToList();
    }
}
