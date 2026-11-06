using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Domain.Bids;
using Microsoft.EntityFrameworkCore;

namespace BidQuiz.Infrastructure.Persistence.Repositories;

public sealed class EfBidRepository(BidQuizDbContext context) : IBidRepository
{
    public Task<Bid?> FindAsync(int bidId, CancellationToken cancellationToken) =>
        context.Bids.FindAsync([bidId], cancellationToken).AsTask();

    public Task<Bid?> FindForTeamAsync(int teamId, int questionId, CancellationToken cancellationToken) =>
        context.Bids.SingleOrDefaultAsync(b => b.TeamId == teamId && b.QuestionId == questionId, cancellationToken);

    public async Task<IReadOnlyList<Bid>> ListForQuestionAsync(int? questionId, CancellationToken cancellationToken) =>
        await context.Bids.AsNoTracking().Where(b => b.QuestionId == questionId).ToListAsync(cancellationToken);

    public Task<int> CountForQuestionAsync(int? questionId, CancellationToken cancellationToken) =>
        context.Bids.CountAsync(b => b.QuestionId == questionId, cancellationToken);

    public Task<int> HighestAmountAsync(int questionId, CancellationToken cancellationToken) =>
        context.Bids.Where(b => b.QuestionId == questionId).MaxAsync(b => b.Amount, cancellationToken);

    public void Add(Bid bid) => context.Bids.Add(bid);

    public Task DeleteAllAsync(CancellationToken cancellationToken) => context.Bids.ExecuteDeleteAsync(cancellationToken);
}
