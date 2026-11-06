using System.Security.Cryptography;

namespace BidQuiz.Application.Common;

/// <summary>Random, unguessable session tokens.</summary>
public static class SessionTokens
{
    private const int TokenBytes = 32;

    public static string New() => Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenBytes));
}
