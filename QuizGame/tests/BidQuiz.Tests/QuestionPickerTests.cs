using BidQuiz.Api.Domain;

namespace BidQuiz.Tests;

public class QuestionPickerTests
{
    private static readonly RandomQuestionPicker Picker = new();

    private static List<QuestionCandidate> Bank(int size) =>
        Enumerable.Range(1, size).Select(i => new QuestionCandidate(i, $"Movie {i}")).ToList();

    [Fact]
    public void PicksTenDifferentQuestionsFromFifty()
    {
        var picked = Picker.Pick(Bank(50), 10);

        Assert.Equal(10, picked.Count);
        Assert.Equal(10, picked.Distinct().Count());
    }

    [Fact]
    public void DifferentGames_GetDifferentQuestions()
    {
        var games = Enumerable.Range(0, 20).Select(_ => string.Join(',', Picker.Pick(Bank(50), 10)));

        Assert.True(games.Distinct().Count() > 1);
    }

    [Fact]
    public void QuestionsWithTheSameAnswer_NeverShareAGame()
    {
        List<QuestionCandidate> bank = [new(1, "Titanic"), new(2, " titanic "), new(3, "Jaws")];

        Assert.Equal(2, Picker.Pick(bank, 3).Count);
    }

    [Fact]
    public void SmallBank_YieldsWhatItHas() =>
        Assert.Equal(3, Picker.Pick(Bank(3), 10).Count);
}
