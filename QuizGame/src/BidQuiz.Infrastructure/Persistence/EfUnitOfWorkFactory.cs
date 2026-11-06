using BidQuiz.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BidQuiz.Infrastructure.Persistence;

public sealed class EfUnitOfWorkFactory(IDbContextFactory<BidQuizDbContext> contextFactory) : IUnitOfWorkFactory
{
    public async Task<IUnitOfWork> BeginAsync(CancellationToken cancellationToken) =>
        new EfUnitOfWork(await contextFactory.CreateDbContextAsync(cancellationToken));
}
