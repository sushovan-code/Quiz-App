using BidQuiz.Application.Abstractions.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BidQuiz.Infrastructure.Persistence.Sqlite;

/// <summary>Creates the SQLite database, or adds the columns an older one is missing.</summary>
public sealed class SqliteDatabaseSchema(IDbContextFactory<BidQuizDbContext> contextFactory) : IDatabaseSchema
{
    // The schema of databases from before configurable team counts. These values are frozen history:
    // they describe what those databases need, not today's defaults.
    private const string TeamColumnsQuery = "SELECT name AS Value FROM pragma_table_info('Teams')";
    private const string RosterColumn = "Active";
    private const string AddRosterColumn = "ALTER TABLE Teams ADD COLUMN Active INTEGER NOT NULL DEFAULT 1";
    private const string AddTeamsPerGameColumn =
        "ALTER TABLE GameStates ADD COLUMN TeamsPerGame INTEGER NOT NULL DEFAULT 10";

    public async Task<SchemaChange> EnsureAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        EnsureDirectory(context.Database.GetConnectionString());
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var columns = await context.Database.SqlQueryRaw<string>(TeamColumnsQuery).ToListAsync(cancellationToken);
        if (columns.Contains(RosterColumn))
        {
            return SchemaChange.None;
        }

        await context.Database.ExecuteSqlRawAsync(AddRosterColumn, cancellationToken);
        await context.Database.ExecuteSqlRawAsync(AddTeamsPerGameColumn, cancellationToken);
        return SchemaChange.AddedTeamRoster;
    }

    private static void EnsureDirectory(string? connectionString)
    {
        var directory = Path.GetDirectoryName(new SqliteConnectionStringBuilder(connectionString).DataSource);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
