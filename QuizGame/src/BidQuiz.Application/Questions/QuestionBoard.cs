using BidQuiz.Application.Abstractions.Persistence;

namespace BidQuiz.Application.Questions;

/// <summary>Reads the host's question board.</summary>
public static class QuestionBoard
{
    public static async Task<IReadOnlyList<QuestionSummaryResponse>> ReadAsync(
        IUnitOfWork work,
        CancellationToken cancellationToken)
    {
        var board = await work.Questions.ListBoardAsync(cancellationToken);
        return board.Select(q => new QuestionSummaryResponse(q.Id, q.Order!.Value, q.IsAsked)).ToList();
    }
}
