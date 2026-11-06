using BidQuiz.Application.Abstractions.Security;
using BidQuiz.Domain.Common;

namespace BidQuiz.Application.Auth.AdminLogout;

/// <summary>
/// Logs the host out by revoking their token. The game itself is untouched, so logging in again
/// resumes the show. Revoking an unknown or already revoked token is not an error.
/// </summary>
public sealed class AdminLogoutHandler(IAdminSessionStore sessions)
{
    public Result Handle(AdminLogout command)
    {
        sessions.Remove(command.Token);
        return Result.Success();
    }
}
