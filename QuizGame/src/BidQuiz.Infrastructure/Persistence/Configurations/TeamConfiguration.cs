using BidQuiz.Domain.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidQuiz.Infrastructure.Persistence.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.Property(t => t.IsActive).HasColumnName("Active");
        builder.Ignore(t => t.Balances);
        builder.HasIndex(t => t.Name).IsUnique();
        builder.HasIndex(t => t.SessionToken);
    }
}
