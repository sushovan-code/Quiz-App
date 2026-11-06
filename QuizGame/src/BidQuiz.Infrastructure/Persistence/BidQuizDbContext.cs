using BidQuiz.Domain.Bids;
using BidQuiz.Domain.Games;
using BidQuiz.Domain.Questions;
using BidQuiz.Domain.Teams;
using Microsoft.EntityFrameworkCore;

namespace BidQuiz.Infrastructure.Persistence;

/// <summary>The EF Core model. Table and column names match the databases created before the layer split.</summary>
public sealed class BidQuizDbContext(DbContextOptions<BidQuizDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<Bid> Bids => Set<Bid>();

    public DbSet<GameState> GameStates => Set<GameState>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite drops DateTimeKind; without this, deadlines would serialize with no offset.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BidQuizDbContext).Assembly);
}
