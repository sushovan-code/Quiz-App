using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Teams;

namespace BidQuiz.Application.Setup.DeleteTeam;

/// <summary>Deleting a playing team brings in the next team on the roster.</summary>
public sealed class DeleteTeamHandler(SetupChanges changes)
{
    public Task<Result<SetupResponse>> HandleAsync(DeleteTeam command, CancellationToken cancellationToken) =>
        changes.ApplyAsync((work, token) => DeleteAsync(work, command.TeamId, token), cancellationToken);

    private static async Task<Result> DeleteAsync(IUnitOfWork work, int teamId, CancellationToken cancellationToken)
    {
        var team = await work.Teams.FindAsync(teamId, cancellationToken);
        if (team is null)
        {
            return TeamErrors.UnknownTeam;
        }

        work.Teams.Remove(team);
        return Result.Success();
    }
}
