using AntFarm.Chinese.Domain.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations;

/// <summary>Bảng access.roles (§5.1.2, migration F3_Access).</summary>
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
