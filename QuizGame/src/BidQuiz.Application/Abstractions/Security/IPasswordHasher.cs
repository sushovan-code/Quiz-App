namespace BidQuiz.Application.Abstractions.Security;

/// <summary>Hashes team passwords. The database only ever stores the hash.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}
