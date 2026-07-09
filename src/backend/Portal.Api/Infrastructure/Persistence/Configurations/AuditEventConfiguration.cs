using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Application.Infrastructure.Persistence.Configurations;

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents");
        builder.HasKey(x => x.AuditEventId);
        builder.Property(x => x.AuditEventId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.ActorName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ActorEmail).HasMaxLength(256);
        builder.Property(x => x.EventType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.CreatedUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(x => new { x.SubmissionId, x.CreatedUtc });
    }
}
