using BidQuiz.Server.Data;
using BidQuiz.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace BidQuiz.Server.Game;

/// <summary>Deals questions from the bank into the game. The caller saves the changes.</summary>
public sealed class QuestionDeck(IQuestionPicker picker)
{
    public async Task DealAsync(GameDbContext db, int count)
    {
        var bank = await db.Questions.ToListAsync();
        foreach (var question in bank)
        {
            question.Order = null;
            question.Asked = false;
        }

        var candidates = bank.Select(q => new QuestionCandidate(q.Id, q.CorrectAnswer)).ToList();
        var order = 1;
        foreach (var id in picker.Pick(candidates, count))
        {
            bank.Single(q => q.Id == id).Order = order++;
        }
    }

    public async Task<IReadOnlyList<QuestionSummaryDto>> ReadBoardAsync(GameDbContext db) =>
        await db.Questions.AsNoTracking()
            .Where(q => q.Order != null)
            .OrderBy(q => q.Order)
            .Select(q => new QuestionSummaryDto(q.Id, q.Order!.Value, q.Asked))
            .ToListAsync();
}
