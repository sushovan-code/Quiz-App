using BidQuiz.Domain.Common;
using BidQuiz.Domain.Games;

namespace BidQuiz.Domain.Teams;

public static class TeamErrors
{
    public static readonly Error UnknownTeam = Error.NotFound("team.unknown", "Unknown team.");

    public static readonly Error RosterFull = Error.Conflict(
        "team.roster_full",
        $"A game can have at most {GameLimits.MaxTeams} teams.");

    public static readonly Error PasswordRequired =
        Error.Validation("team.password_required", "Please enter a team password.");

    public static readonly Error NameRequired = Error.Validation("team.name_required", "Please enter a team name.");

    public static readonly Error NameTooLong = Error.Validation(
        "team.name_too_long",
        $"Team names are limited to {GameLimits.MaxNameLength} characters.");

    public static readonly Error NameTaken =
        Error.Conflict("team.name_taken", "There is already a team with that name.");
}
