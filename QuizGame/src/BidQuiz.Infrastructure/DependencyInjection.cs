using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Presence;
using BidQuiz.Application.Abstractions.Security;
using BidQuiz.Application.Abstractions.Seeding;
using BidQuiz.Infrastructure.Persistence;
using BidQuiz.Infrastructure.Persistence.Sqlite;
using BidQuiz.Infrastructure.Presence;
using BidQuiz.Infrastructure.Security;
using BidQuiz.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BidQuiz.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the adapters. <c>Database:Provider</c> chooses where the game is stored.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()?.Provider
            ?? DatabaseProviders.Sqlite;

        switch (provider)
        {
            case DatabaseProviders.Sqlite:
                services.AddSqlitePersistence();
                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown {DatabaseOptions.SectionName}:{nameof(DatabaseOptions.Provider)} '{provider}'. " +
                    $"Supported: {DatabaseProviders.Sqlite}.");
        }

        services.AddSingleton<ISeedDataSource, JsonSeedDataSource>();
        services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();
        services.AddSingleton<IAdminSessionStore, InMemoryAdminSessionStore>();
        services.AddSingleton<ITeamPresence, InMemoryTeamPresence>();
        return services;
    }

    private static void AddSqlitePersistence(this IServiceCollection services)
    {
        // The connection string is read when the first context is made, so test hosts can override it.
        services.AddDbContextFactory<BidQuizDbContext>((provider, options) => options.UseSqlite(
            provider.GetRequiredService<IConfiguration>().GetConnectionString(DatabaseOptions.ConnectionStringName)));
        services.AddSingleton<IUnitOfWorkFactory, EfUnitOfWorkFactory>();
        services.AddSingleton<IDatabaseSchema, SqliteDatabaseSchema>();
    }
}
