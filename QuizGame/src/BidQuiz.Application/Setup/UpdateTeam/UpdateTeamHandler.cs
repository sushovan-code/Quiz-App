using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Security;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Teams;

namespace BidQuiz.Application.Setup.UpdateTeam;

public sealed class UpdateTeamHandler(SetupChanges changes, IPasswordHasher hasher)
{
    public Task<Result<SetupResponse>> HandleAsync(UpdateTeam command, CancellationToken cancellationToken) =>
        changes.ApplyAsync((work, token) => UpdateAsync(work, command, token), cancellationToken);

    private async Task<Result> UpdateAsync(IUnitOfWork work, UpdateTeam command, CancellationToken cancellationToken)
    {
        var teams = await work.Teams.ListAsync(cancellationToken);
        var team = teams.SingleOrDefault(t => t.Id == command.TeamId);
        if (team is null)
        {
            return TeamErrors.UnknownTeam;
        }

        var name = TeamName.Normalize(command.Team.Name);
        if (name.IsFailure)
        {
            return name;
        }

        if (TeamName.IsTaken(teams, name.Value, team.Id))
        {
            return TeamErrors.NameTaken;
        }

        team.Rename(name.Value);
        if (!string.IsNullOrWhiteSpace(command.Team.Password))
        {
            team.ChangePasswordHash(hasher.Hash(command.Team.Password));
        }

        return Result.Success();
    }
}
