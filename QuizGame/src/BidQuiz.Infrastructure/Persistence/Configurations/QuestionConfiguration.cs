using BidQuiz.Domain.Questions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidQuiz.Infrastructure.Persistence.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.Property(q => q.IsAsked).HasColumnName("Asked");
        builder.Ignore(q => q.IsInGame);
        builder.HasIndex(q => q.Text).IsUnique();
    }
}
