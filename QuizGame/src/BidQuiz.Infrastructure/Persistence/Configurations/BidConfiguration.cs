using BidQuiz.Domain.Bids;
using BidQuiz.Domain.Questions;
using BidQuiz.Domain.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidQuiz.Infrastructure.Persistence.Configurations;

public sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        // Relationships without navigation properties keep the domain free of persistence concerns.
        builder.HasOne<Team>().WithMany().HasForeignKey(b => b.TeamId);
        builder.HasOne<Question>().WithMany().HasForeignKey(b => b.QuestionId);
        builder.HasIndex(b => new { b.TeamId, b.QuestionId }).IsUnique();
    }
}
