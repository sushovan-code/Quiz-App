using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BidQuiz.Infrastructure.Persistence.Sqlite;

/// <summary>Lets <c>dotnet ef</c> build the model without starting the app. Never used at runtime.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BidQuizDbContext>
{
    private const string DesignTimeConnectionString = "Data Source=design-time.db";

    public BidQuizDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<BidQuizDbContext>().UseSqlite(DesignTimeConnectionString).Options);
}
