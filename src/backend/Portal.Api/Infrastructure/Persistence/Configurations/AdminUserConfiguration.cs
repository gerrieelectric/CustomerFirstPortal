using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portal.Domain.Entities;

namespace Portal.Application.Infrastructure.Persistence.Configurations;

public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("AdminUsers");
        builder.HasKey(x => x.AdminUserId);

        builder.Property(x => x.EntraObjectId).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Department).HasMaxLength(100);
        builder.Property(x => x.CreatedUtc).HasColumnType("datetime2(0)");
        builder.Property(x => x.UpdatedUtc).HasColumnType("datetime2(0)");

        builder.HasIndex(x => x.EntraObjectId).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
