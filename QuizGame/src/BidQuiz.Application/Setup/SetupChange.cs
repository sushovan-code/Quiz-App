using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Domain.Common;

namespace BidQuiz.Application.Setup;

/// <summary>One change to the setup, run by <see cref="SetupChanges"/>.</summary>
public delegate Task<Result> SetupChange(IUnitOfWork work, CancellationToken cancellationToken);
