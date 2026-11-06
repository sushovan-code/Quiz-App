using BidQuiz.Application.Abstractions.Persistence;

namespace BidQuiz.Application.Setup.ListTeamOptions;

/// <summary>The teams a device can log in as: the ones playing, by name.</summary>
public sealed class ListTeamOptionsHandler(IUnitOfWorkFactory unitOfWork)
{
    public async Task<IReadOnlyList<TeamOptionResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        await using var work = await unitOfWork.BeginAsync(cancellationToken);
        var teams = await work.Teams.ListActiveAsync(cancellationToken);
        return teams
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .Select(t => new TeamOptionResponse(t.Id, t.Name))
            .ToList();
    }
}
