using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BidQuiz.Api.Data;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<GameState> GameStates => Set<GameState>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite drops DateTimeKind; without this, deadlines would serialize with no offset.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>().HasIndex(t => t.Name).IsUnique();
        modelBuilder.Entity<Team>().HasIndex(t => t.SessionToken);
        modelBuilder.Entity<Bid>().HasIndex(b => new { b.TeamId, b.QuestionId }).IsUnique();
        modelBuilder.Entity<Question>().HasIndex(q => q.Text).IsUnique();
    }

    private sealed class UtcConverter() : ValueConverter<DateTime, DateTime>(
        v => v,
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
