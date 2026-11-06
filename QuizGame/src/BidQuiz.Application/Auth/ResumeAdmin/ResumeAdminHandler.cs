using BidQuiz.Application.Abstractions.Security;
using BidQuiz.Domain.Common;

namespace BidQuiz.Application.Auth.ResumeAdmin;

public sealed class ResumeAdminHandler(IAdminSessionStore sessions)
{
    public Result Handle(ResumeAdmin command) =>
        sessions.Contains(command.Token) ? Result.Success() : AuthErrors.SessionExpired;
}
