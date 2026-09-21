using AntFarm.Cms.Domain.Site;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Cms.Infrastructure.Persistence.Configurations.Site;

/// <summary>Bảng site.settings (§5.1.2, migration W3a_SiteBasics).</summary>
public sealed class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.ToTable("settings", "site");

        builder.HasKey(s => s.Key);
        builder.Property(s => s.Key).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Value).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
        builder.Property(s => s.UpdatedBy);
    }
}
