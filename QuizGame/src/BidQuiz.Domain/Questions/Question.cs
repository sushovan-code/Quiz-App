namespace BidQuiz.Domain.Questions;

/// <summary>One entry in the question bank. Only the questions dealt into the game have an Order.</summary>
public sealed class Question
{
    private Question()
    {
    }

    public int Id { get; private set; }

    /// <summary>Position in the current game, or null while the question stays in the bank.</summary>
    public int? Order { get; private set; }

    public string Category { get; private set; } = "";

    public string Text { get; private set; } = "";

    public string CorrectAnswer { get; private set; } = "";

    public string? ImageUrl { get; private set; }

    /// <summary>Set when the question is started, so it stays greyed out even if nobody answered.</summary>
    public bool IsAsked { get; private set; }

    public bool IsInGame => Order is not null;

    public static Question Create(QuestionContent content)
    {
        var question = new Question();
        question.Edit(content);
        return question;
    }

    public void Edit(QuestionContent content)
    {
        Category = content.Category;
        Text = content.Text;
        CorrectAnswer = content.CorrectAnswer;
        ImageUrl = content.ImageUrl;
    }

    public void PlaceInGame(int order) => Order = order;

    public void RemoveFromGame()
    {
        Order = null;
        IsAsked = false;
    }

    public void MarkAsked() => IsAsked = true;
}
