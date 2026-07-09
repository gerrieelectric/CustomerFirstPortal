using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Application.Infrastructure.Persistence.Configurations;

public class GerrieSyncEventConfiguration : IEntityTypeConfiguration<GerrieSyncEvent>
{
    public void Configure(EntityTypeBuilder<GerrieSyncEvent> builder)
    {
        builder.ToTable("GerrieSyncEvents");
        builder.HasKey(x => x.GerrieSyncEventId);
        builder.Property(x => x.GerrieSyncEventId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.SyncStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ResponseCode).HasMaxLength(50);
        builder.Property(x => x.ResponseMessage).HasMaxLength(4000);
        builder.Property(x => x.ExternalRecordId).HasMaxLength(100);
        builder.Property(x => x.CreatedUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(x => new { x.SubmissionId, x.CreatedUtc });
    }
}
