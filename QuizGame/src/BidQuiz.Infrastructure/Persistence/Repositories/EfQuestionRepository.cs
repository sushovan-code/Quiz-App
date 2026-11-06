using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Domain.Questions;
using Microsoft.EntityFrameworkCore;

namespace BidQuiz.Infrastructure.Persistence.Repositories;

public sealed class EfQuestionRepository(BidQuizDbContext context) : IQuestionRepository
{
    /// <summary>SQLite's case-insensitive collation, matching the lower() comparison the bank always used.</summary>
    private const string CaseInsensitiveCollation = "NOCASE";

    public Task<Question?> FindAsync(int questionId, CancellationToken cancellationToken) =>
        context.Questions.FindAsync([questionId], cancellationToken).AsTask();

    public async Task<IReadOnlyList<Question>> ListAsync(CancellationToken cancellationToken) =>
        await context.Questions.OrderBy(q => q.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Question>> ListBoardAsync(CancellationToken cancellationToken) =>
        await context.Questions.AsNoTracking()
            .Where(q => q.Order != null)
            .OrderBy(q => q.Order)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> ListTextsAsync(CancellationToken cancellationToken) =>
        await context.Questions.Select(q => q.Text).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken) => context.Questions.CountAsync(cancellationToken);

    public Task<int> CountInGameAsync(CancellationToken cancellationToken) =>
        context.Questions.CountAsync(q => q.Order != null, cancellationToken);

    public Task<bool> AnyAskedAsync(CancellationToken cancellationToken) =>
        context.Questions.AnyAsync(q => q.IsAsked, cancellationToken);

    public Task<bool> ExistsWithTextAsync(string text, int? exceptQuestionId, CancellationToken cancellationToken) =>
        context.Questions
            .Where(q => exceptQuestionId == null || q.Id != exceptQuestionId)
            .AnyAsync(q => EF.Functions.Collate(q.Text, CaseInsensitiveCollation) == text, cancellationToken);

    public void Add(Question question) => context.Questions.Add(question);

    public void Remove(Question question) => context.Questions.Remove(question);
}
