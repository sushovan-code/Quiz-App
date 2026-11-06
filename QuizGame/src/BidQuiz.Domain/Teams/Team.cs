using BidQuiz.Domain.Scoring;

namespace BidQuiz.Domain.Teams;

/// <summary>A team on the roster. Several devices may play as one team.</summary>
public sealed class Team
{
    private Team()
    {
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = "";

    public string PasswordHash { get; private set; } = "";

    public string Color { get; private set; } = "";

    public int BidBalance { get; private set; }

    public int Wallet { get; private set; }

    /// <summary>False for teams on the roster that sit this game out. See <see cref="TeamRoster"/>.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Lets a phone resume after a refresh or a server restart.</summary>
    public string? SessionToken { get; private set; }

    public Balances Balances => new(BidBalance, Wallet);

    public static Team Create(string name, string color, string passwordHash, Balances opening) => new()
    {
        Name = name,
        Color = color,
        PasswordHash = passwordHash,
        BidBalance = opening.BidBalance,
        Wallet = opening.Wallet
    };

    public void Rename(string name) => Name = name;

    public void ChangePasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public void SetBalances(Balances balances)
    {
        BidBalance = balances.BidBalance;
        Wallet = balances.Wallet;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    /// <summary>Returns the team's session token, creating it on the first login.</summary>
    public string StartSession(Func<string> newToken) => SessionToken ??= newToken();
}
