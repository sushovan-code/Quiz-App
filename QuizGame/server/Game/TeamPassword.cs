using BidQuiz.Server.Data;
using Microsoft.AspNetCore.Identity;

namespace BidQuiz.Server.Game;

public static class TeamPassword
{
    private static readonly PasswordHasher<Team> Hasher = new();

    public static void Set(Team team, string password) =>
        team.PasswordHash = Hasher.HashPassword(team, password);

    public static bool Matches(Team team, string password) =>
        Hasher.VerifyHashedPassword(team, team.PasswordHash, password) != PasswordVerificationResult.Failed;
}
