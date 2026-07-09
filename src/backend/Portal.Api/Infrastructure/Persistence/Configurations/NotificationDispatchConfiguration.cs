using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Application.Infrastructure.Persistence.Configurations;

public class NotificationDispatchConfiguration : IEntityTypeConfiguration<NotificationDispatch>
{
    public void Configure(EntityTypeBuilder<NotificationDispatch> builder)
    {
        builder.ToTable("NotificationDispatches");
        builder.HasKey(x => x.NotificationDispatchId);
        builder.Property(x => x.NotificationDispatchId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.NotificationType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.RecipientEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.RecipientDisplayName).HasMaxLength(200);
        builder.Property(x => x.CorrelationId).HasMaxLength(64);
        builder.Property(x => x.CreatedUtc).HasColumnType("datetime2(0)");
        builder.Property(x => x.NextAttemptUtc).HasColumnType("datetime2(0)");
        builder.Property(x => x.LastAttemptUtc).HasColumnType("datetime2(0)");
        builder.Property(x => x.SentUtc).HasColumnType("datetime2(0)");
        builder.Property(x => x.LastError).HasMaxLength(4000);

        builder.HasOne(x => x.Submission)
            .WithMany()
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.Status, x.NextAttemptUtc });
        builder.HasIndex(x => new { x.SubmissionId, x.CreatedUtc });
    }
}
