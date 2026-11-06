using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Games;

namespace BidQuiz.Application.Setup.SetTeamsPerGame;

/// <summary>The first <c>Count</c> teams on the roster play; the rest sit out and can't log in.</summary>
public sealed class SetTeamsPerGameHandler(SetupChanges changes)
{
    public Task<Result<SetupResponse>> HandleAsync(SetTeamsPerGame command, CancellationToken cancellationToken) =>
        changes.ApplyAsync((work, token) => SetAsync(work, command.Count, token), cancellationToken);

    private static async Task<Result> SetAsync(IUnitOfWork work, int count, CancellationToken cancellationToken)
    {
        if (count < 1 || count > await work.Teams.CountAsync(cancellationToken))
        {
            return GameErrors.TeamsPerGameOutOfRange;
        }

        var state = await work.Games.GetAsync(cancellationToken);
        state.SetTeamsPerGame(count);
        return Result.Success();
    }
}
