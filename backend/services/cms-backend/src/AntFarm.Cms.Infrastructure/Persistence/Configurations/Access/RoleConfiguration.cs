using AntFarm.Cms.Domain.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Cms.Infrastructure.Persistence.Configurations.Access;

/// <summary>Bảng access.roles (§5.1.1, migration W1_Access).</summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", "access");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).HasMaxLength(32).IsRequired();
        builder.HasIndex(r => r.Code).IsUnique();

        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
    }
}
