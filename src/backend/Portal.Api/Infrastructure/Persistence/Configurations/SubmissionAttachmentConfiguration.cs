using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Application.Infrastructure.Persistence.Configurations;

public class SubmissionAttachmentConfiguration : IEntityTypeConfiguration<SubmissionAttachment>
{
    public void Configure(EntityTypeBuilder<SubmissionAttachment> builder)
    {
        builder.ToTable("SubmissionAttachments");
        builder.HasKey(x => x.SubmissionAttachmentId);
        builder.Property(x => x.SubmissionAttachmentId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.BlobName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.UploadedByName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UploadedUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(x => x.SubmissionId);
    }
}
