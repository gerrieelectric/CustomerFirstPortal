using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Application.Infrastructure.Persistence.Configurations;

public class SubmissionReviewConfiguration : IEntityTypeConfiguration<SubmissionReview>
{
    public void Configure(EntityTypeBuilder<SubmissionReview> builder)
    {
        builder.ToTable("SubmissionReviews");
        builder.HasKey(x => x.SubmissionReviewId);
        builder.Property(x => x.SubmissionReviewId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.Decision)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ReviewNotes).HasMaxLength(2000);
        builder.Property(x => x.ReviewedUtc).HasColumnType("datetime2(0)");

        builder.HasOne(x => x.AdminUser)
            .WithMany(a => a.Reviews)
            .HasForeignKey(x => x.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.SubmissionId, x.ReviewedUtc });
    }
}
