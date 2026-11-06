namespace BidQuiz.Application.Abstractions.Persistence;

/// <summary>Creates or upgrades the database's structure before the game uses it.</summary>
public interface IDatabaseSchema
{
    Task<SchemaChange> EnsureAsync(CancellationToken cancellationToken);
}
