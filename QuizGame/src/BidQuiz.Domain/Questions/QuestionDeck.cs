namespace BidQuiz.Domain.Questions;

/// <summary>Deals questions from the bank into the game.</summary>
public static class QuestionDeck
{
    /// <summary>Takes every question out of the game, then places the picked ones in order from 1.</summary>
    public static void Deal(IReadOnlyList<Question> bank, int count, IQuestionPicker picker)
    {
        foreach (var question in bank)
        {
            question.RemoveFromGame();
        }

        var candidates = bank.Select(q => new QuestionCandidate(q.Id, q.CorrectAnswer)).ToList();
        var order = 1;
        foreach (var id in picker.Pick(candidates, count))
        {
            bank.Single(q => q.Id == id).PlaceInGame(order++);
        }
    }
}
