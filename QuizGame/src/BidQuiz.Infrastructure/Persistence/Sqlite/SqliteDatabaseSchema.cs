using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Infrastructure.Persistence.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BidQuiz.Infrastructure.Persistence.Sqlite;

/// <summary>
/// Brings the SQLite database to the current schema with EF Core migrations. Databases made before
/// migrations (by EnsureCreated) are first brought up to the InitialCreate schema and marked as
/// having it, so their data is kept and later migrations apply normally.
/// </summary>
public sealed class SqliteDatabaseSchema(IDbContextFactory<BidQuizDbContext> contextFactory) : IDatabaseSchema
{
    // The schema of databases from before configurable team counts. These values are frozen history:
    // they describe what those databases need, not today's defaults.
    private const string TableExistsQuery =
        "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name = {0}";
    private const string TeamColumnsQuery = "SELECT name AS Value FROM pragma_table_info('Teams')";
    private const string TeamsTable = "Teams";
    private const string RosterColumn = "Active";
    private const string AddRosterColumn = "ALTER TABLE Teams ADD COLUMN Active INTEGER NOT NULL DEFAULT 1";
    private const string AddTeamsPerGameColumn =
        "ALTER TABLE GameStates ADD COLUMN TeamsPerGame INTEGER NOT NULL DEFAULT 10";

    public async Task<SchemaChange> EnsureAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        EnsureDirectory(context.Database.GetConnectionString());

        var change = SchemaChange.None;
        if (await TableExistsAsync(context, TeamsTable, cancellationToken))
        {
            change = await AddTeamRosterAsync(context, cancellationToken);
            await BaselineAsync(context, cancellationToken);
        }

        await context.Database.MigrateAsync(cancellationToken);
        return change;
    }

    /// <summary>Adds the roster columns to a database from before team counts.</summary>
    private static async Task<SchemaChange> AddTeamRosterAsync(BidQuizDbContext context, CancellationToken cancellationToken)
    {
        var columns = await context.Database.SqlQueryRaw<string>(TeamColumnsQuery).ToListAsync(cancellationToken);
        if (columns.Contains(RosterColumn))
        {
            return SchemaChange.None;
        }

        await context.Database.ExecuteSqlRawAsync(AddRosterColumn, cancellationToken);
        await context.Database.ExecuteSqlRawAsync(AddTeamsPerGameColumn, cancellationToken);
        return SchemaChange.AddedTeamRoster;
    }

    /// <summary>Records InitialCreate as applied on a database that EnsureCreated made.</summary>
    private static async Task BaselineAsync(BidQuizDbContext context, CancellationToken cancellationToken)
    {
        var history = context.GetService<IHistoryRepository>();
        if (await history.ExistsAsync(cancellationToken))
        {
            return;
        }

        var migrations = context.GetService<IMigrationsAssembly>();
        var initialCreate = migrations.Migrations.Keys.Single(id => id.EndsWith(InitialCreateName, StringComparison.Ordinal));
        await context.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript(), cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            history.GetInsertScript(new HistoryRow(initialCreate, ProductInfo.GetVersion())),
            cancellationToken);
    }

    private static string InitialCreateName => "_" + nameof(InitialCreate);

    private static async Task<bool> TableExistsAsync(
        BidQuizDbContext context,
        string table,
        CancellationToken cancellationToken) =>
        await context.Database.SqlQueryRaw<string>(TableExistsQuery, table).AnyAsync(cancellationToken);

    private static void EnsureDirectory(string? connectionString)
    {
        var directory = Path.GetDirectoryName(new SqliteConnectionStringBuilder(connectionString).DataSource);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
