using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Application.Infrastructure.Persistence.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("Submissions");
        builder.HasKey(x => x.SubmissionId);
        builder.Property(x => x.SubmissionId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.SubmissionType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.SubmittedByName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SubmittedByEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SubmittedByDepartment).HasMaxLength(100);
        builder.Property(x => x.NomineeName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NomineeEmail).HasMaxLength(256);
        builder.Property(x => x.NomineeDepartment).HasMaxLength(100);
        builder.Property(x => x.CompanyName).HasMaxLength(200);
        builder.Property(x => x.CustomerName).HasMaxLength(200);
        builder.Property(x => x.CustomerEmail).HasMaxLength(256);
        builder.Property(x => x.RecognitionReason).IsRequired();
        builder.Property(x => x.GerrieRecordId).HasMaxLength(100);
        builder.Property(x => x.SubmittedUtc).HasColumnType("datetime2(0)");
        builder.Property(x => x.ApprovedUtc).HasColumnType("datetime2(0)");
        builder.Property(x => x.RejectedUtc).HasColumnType("datetime2(0)");
        builder.Property(x => x.LastGerrieSyncUtc).HasColumnType("datetime2(0)");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.ReviewedByAdmin)
            .WithMany(a => a.ReviewedSubmissions)
            .HasForeignKey(x => x.ReviewedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Attachments)
            .WithOne(a => a.Submission)
            .HasForeignKey(a => a.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Reviews)
            .WithOne(r => r.Submission)
            .HasForeignKey(r => r.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.GerrieSyncEvents)
            .WithOne(g => g.Submission)
            .HasForeignKey(g => g.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AuditEvents)
            .WithOne(a => a.Submission)
            .HasForeignKey(a => a.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.Status, x.SubmittedUtc });
        builder.HasIndex(x => x.NomineeName);
        builder.HasIndex(x => x.SubmittedByEmail);
        builder.HasIndex(x => x.ReviewedByAdminUserId).HasFilter("[ReviewedByAdminUserId] IS NOT NULL");
    }
}
