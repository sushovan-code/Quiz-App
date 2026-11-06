using System.ComponentModel.DataAnnotations;

namespace BidQuiz.Infrastructure.Persistence;

/// <summary>Bound to the <c>Database</c> configuration section.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>The connection string name, under <c>ConnectionStrings</c>.</summary>
    public const string ConnectionStringName = "Game";

    /// <summary>Which adapter stores the game. See <see cref="DatabaseProviders"/>.</summary>
    [Required]
    public string Provider { get; set; } = DatabaseProviders.Sqlite;
}
