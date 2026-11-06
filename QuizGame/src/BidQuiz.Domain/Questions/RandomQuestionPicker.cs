namespace BidQuiz.Domain.Questions;

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
