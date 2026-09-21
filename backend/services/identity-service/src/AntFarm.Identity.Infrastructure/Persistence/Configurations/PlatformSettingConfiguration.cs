using AntFarm.Identity.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Identity.Infrastructure.Persistence.Configurations;

/// <summary>Bảng identity.settings (§5.1.7, migration W10_Settings).</summary>
public sealed class PlatformSettingConfiguration : IEntityTypeConfiguration<PlatformSetting>
{
    public void Configure(EntityTypeBuilder<PlatformSetting> builder)
    {
        builder.ToTable("settings", "identity");

        builder.HasKey(s => s.Key);
        builder.Property(s => s.Key).HasMaxLength(64).IsRequired();

        builder.Property(s => s.Value).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
    }
}
