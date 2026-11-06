using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Domain.Common;

namespace BidQuiz.Application.Auth.ResumeTeam;

/// <summary>Works out which team a returning device is, with no password. Benched teams can't resume.</summary>
public sealed class ResumeTeamHandler(IUnitOfWorkFactory unitOfWork)
{
    public async Task<Result<int>> HandleAsync(ResumeTeam command, CancellationToken cancellationToken)
    {
        await using var work = await unitOfWork.BeginAsync(cancellationToken);
        var team = await work.Teams.FindActiveBySessionTokenAsync(command.Token, cancellationToken);
        return team is null ? AuthErrors.SessionExpired : team.Id;
    }
}
