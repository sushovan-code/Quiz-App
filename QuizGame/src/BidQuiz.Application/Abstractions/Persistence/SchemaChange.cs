namespace BidQuiz.Application.Abstractions.Persistence;

/// <summary>What <see cref="IDatabaseSchema.EnsureAsync"/> had to change in an existing database.</summary>
public enum SchemaChange
{
    None = 0,

    /// <summary>A database from before configurable team counts gained them, so its data needs bringing up to date.</summary>
    AddedTeamRoster = 1
}
