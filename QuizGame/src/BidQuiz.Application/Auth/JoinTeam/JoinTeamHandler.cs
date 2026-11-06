using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Security;
using BidQuiz.Application.Common;
using BidQuiz.Domain.Common;

namespace BidQuiz.Application.Auth.JoinTeam;

/// <summary>Logs a device in as a team. Every device of a team shares one session token.</summary>
public sealed class JoinTeamHandler(IUnitOfWorkFactory unitOfWork, GameGate gate, IPasswordHasher hasher)
{
    public async Task<Result<TeamCredentials>> HandleAsync(JoinTeam command, CancellationToken cancellationToken)
    {
        using var lease = await gate.EnterAsync(cancellationToken);
        await using var work = await unitOfWork.BeginAsync(cancellationToken);

        var team = await work.Teams.FindAsync(command.TeamId, cancellationToken);
        if (team is null || !hasher.Verify(team.PasswordHash, command.Password))
        {
            return AuthErrors.WrongTeamOrPassword;
        }

        if (!team.IsActive)
        {
            return AuthErrors.TeamNotPlaying;
        }

        var token = team.StartSession(SessionTokens.New);
        await work.SaveChangesAsync(cancellationToken);
        return new TeamCredentials(team.Id, token);
    }
}
