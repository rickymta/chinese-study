using AntFarm.Cms.Domain.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Cms.Infrastructure.Persistence.Configurations.Access;

/// <summary>Bảng access.permissions (§5.1.1, migration W1_Access) — khoá chính là mã quyền.</summary>
public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions", "access");

        builder.HasKey(p => p.Code);
        builder.Property(p => p.Code).HasMaxLength(64);

        builder.Property(p => p.Description).HasMaxLength(200).IsRequired();
    }
}
