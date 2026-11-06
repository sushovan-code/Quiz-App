using BidQuiz.Application.Abstractions.Security;
using BidQuiz.Domain.Teams;
using Microsoft.AspNetCore.Identity;

namespace BidQuiz.Infrastructure.Security;

/// <summary>ASP.NET Core Identity's PBKDF2 hasher, so hashes written before the layer split still verify.</summary>
public sealed class AspNetCorePasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<Team> _hasher = new();

    // The Identity hasher's user parameter is unused by its hash format.
    private static readonly Team NoUser = null!;

    public string Hash(string password) => _hasher.HashPassword(NoUser, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(NoUser, passwordHash, password) != PasswordVerificationResult.Failed;
}
