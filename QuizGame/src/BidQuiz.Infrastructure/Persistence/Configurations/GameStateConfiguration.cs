using BidQuiz.Domain.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidQuiz.Infrastructure.Persistence.Configurations;

public sealed class GameStateConfiguration : IEntityTypeConfiguration<GameState>
{
    public void Configure(EntityTypeBuilder<GameState> builder)
    {
        builder.Property(s => s.IsAnswerRevealed).HasColumnName("AnswerRevealed");
        builder.Ignore(s => s.OpenDeadline);
        builder.Ignore(s => s.IsAccepting);
        builder.Ignore(s => s.AreBidsRevealed);
    }
}
