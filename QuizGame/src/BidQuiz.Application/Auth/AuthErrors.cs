using BidQuiz.Domain.Common;

namespace BidQuiz.Application.Auth;

public static class AuthErrors
{
    public static readonly Error WrongTeamOrPassword =
        Error.Unauthorized("auth.wrong_team_or_password", "Wrong team or password.");

    public static readonly Error TeamNotPlaying =
        Error.Unauthorized("auth.team_not_playing", "That team isn't playing in this game.");

    /// <summary>Clients look for the "Session expired" prefix to send the device back to login.</summary>
    public static readonly Error SessionExpired =
        Error.Unauthorized("auth.session_expired", "Session expired. Please log in again.");

    public static readonly Error WrongAdminPassword =
        Error.Unauthorized("auth.wrong_admin_password", "Wrong admin password.");

    public static readonly Error TeamLoginRequired =
        Error.Unauthorized("auth.team_login_required", "Please log in with your team first.");

    public static readonly Error AdminOnly = Error.Unauthorized("auth.admin_only", "Admin only.");
}
