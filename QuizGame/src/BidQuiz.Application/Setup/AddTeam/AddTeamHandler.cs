using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Security;
using BidQuiz.Domain.Common;
using BidQuiz.Domain.Games;
using BidQuiz.Domain.Scoring;
using BidQuiz.Domain.Teams;

namespace BidQuiz.Application.Setup.AddTeam;

/// <summary>A new team goes to the end of the roster and gets a colour no other team is using.</summary>
public sealed class AddTeamHandler(SetupChanges changes, IPasswordHasher hasher, IScoringPolicy scoring)
{
    public Task<Result<SetupResponse>> HandleAsync(AddTeam command, CancellationToken cancellationToken) =>
        changes.ApplyAsync((work, token) => AddAsync(work, command.Team, token), cancellationToken);

    private async Task<Result> AddAsync(IUnitOfWork work, SaveTeamRequest request, CancellationToken cancellationToken)
    {
        var teams = await work.Teams.ListAsync(cancellationToken);
        if (teams.Count >= GameLimits.MaxTeams)
        {
            return TeamErrors.RosterFull;
        }

        var name = TeamName.Normalize(request.Name);
        if (name.IsFailure)
        {
            return name;
        }

        if (TeamName.IsTaken(teams, name.Value, ownId: null))
        {
            return TeamErrors.NameTaken;
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return TeamErrors.PasswordRequired;
        }

        var color = TeamPalette.FirstFree(teams.Select(t => t.Color));
        work.Teams.Add(Team.Create(name.Value, color, hasher.Hash(request.Password), scoring.Opening));
        return Result.Success();
    }
}
