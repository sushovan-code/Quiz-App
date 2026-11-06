using BidQuiz.Domain.Common;
using BidQuiz.Domain.Games;

namespace BidQuiz.Domain.Teams;

/// <summary>The rules for team names.</summary>
public static class TeamName
{
    public static Result<string> Normalize(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return TeamErrors.NameRequired;
        }

        return trimmed.Length > GameLimits.MaxNameLength ? TeamErrors.NameTooLong : trimmed;
    }

    /// <summary>Names are unique regardless of case. <paramref name="ownId"/> lets a team keep its own name.</summary>
    public static bool IsTaken(IEnumerable<Team> teams, string name, int? ownId) =>
        teams.Any(t => t.Id != ownId && string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
}
