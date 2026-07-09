using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Application.Infrastructure.Persistence.Configurations;

public class SubmissionSuggestionConfiguration : IEntityTypeConfiguration<SubmissionSuggestion>
{
    public void Configure(EntityTypeBuilder<SubmissionSuggestion> builder)
    {
        builder.ToTable("SubmissionSuggestions");
        builder.HasKey(x => x.SubmissionSuggestionId);
        builder.Property(x => x.SubmissionSuggestionId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.SubmissionId).IsRequired();
        builder.Property(x => x.ReasonHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OriginalReason).IsRequired();
        builder.Property(x => x.FullEditedReason).IsRequired();
        builder.Property(x => x.SuggestionsJson).IsRequired();
        builder.Property(x => x.ModelName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PromptVersion).IsRequired();
        builder.Property(x => x.GeneratedUtc).HasColumnType("datetime2(0)").IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.Submission)
            .WithMany(s => s.Suggestions)
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for fast lookup by submission
        builder.HasIndex(x => x.SubmissionId);

        // Index for detecting reason changes
        builder.HasIndex(x => new { x.SubmissionId, x.ReasonHash });
    }
}
