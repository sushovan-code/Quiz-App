using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using BidQuiz.Server.Data;
using BidQuiz.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BidQuiz.Server.Game;

/// <summary>Team tokens live in the database. Admin tokens are in memory, so a restart asks the host to log in.</summary>
public sealed class AuthService(
    IDbContextFactory<GameDbContext> dbFactory,
    GameGate gate,
    IOptions<GameOptions> gameOptions) : IAuthService
{
    private readonly GameOptions _game = gameOptions.Value;
    private readonly ConcurrentDictionary<string, byte> _adminTokens = new();

    public async Task<(int TeamId, string Token)> LoginTeamAsync(int teamId, string password)
    {
        using (await gate.EnterAsync())
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var team = await db.Teams.FindAsync(teamId);
            if (team is null || !TeamPassword.Matches(team, password))
                throw new GameRuleException("Wrong team or password.");
            if (!team.Active)
                throw new GameRuleException("That team isn't playing in this game.");

            team.SessionToken ??= NewToken();
            await db.SaveChangesAsync();
            return (team.Id, team.SessionToken);
        }
    }

    public async Task<int> ResolveTeamTokenAsync(string token)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var teamId = await db.Teams.Where(t => t.SessionToken == token && t.Active).Select(t => (int?)t.Id).SingleOrDefaultAsync();
        return teamId ?? throw new GameRuleException("Session expired. Please log in again.");
    }

    public string LoginAdmin(string password)
    {
        var expected = Encoding.UTF8.GetBytes(_game.AdminPassword);
        var actual = Encoding.UTF8.GetBytes(password);
        if (expected.Length == 0 || !CryptographicOperations.FixedTimeEquals(expected, actual))
            throw new GameRuleException("Wrong admin password.");

        var token = NewToken();
        _adminTokens[token] = 0;
        return token;
    }

    public void ValidateAdminToken(string token)
    {
        if (!_adminTokens.ContainsKey(token))
            throw new GameRuleException("Session expired. Please log in again.");
    }

    private static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}
