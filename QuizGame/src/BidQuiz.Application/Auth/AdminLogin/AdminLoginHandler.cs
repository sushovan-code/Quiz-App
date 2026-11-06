using System.Security.Cryptography;
using System.Text;
using BidQuiz.Application.Abstractions.Security;
using BidQuiz.Application.Common;
using BidQuiz.Application.Options;
using BidQuiz.Domain.Common;
using Microsoft.Extensions.Options;

namespace BidQuiz.Application.Auth.AdminLogin;

/// <summary>Logs the host in. The comparison takes the same time whatever the guess.</summary>
public sealed class AdminLoginHandler(IOptions<GameOptions> options, IAdminSessionStore sessions)
{
    public Result<string> Handle(AdminLogin command)
    {
        var expected = Encoding.UTF8.GetBytes(options.Value.AdminPassword);
        var actual = Encoding.UTF8.GetBytes(command.Password);
        if (expected.Length == 0 || !CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            return AuthErrors.WrongAdminPassword;
        }

        var token = SessionTokens.New();
        sessions.Add(token);
        return token;
    }
}
