using AntFarm.Cms.Domain.Site;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Cms.Infrastructure.Persistence.Configurations.Site;

/// <summary>Bảng site.languages (§5.1.2, migration W3a_SiteBasics).</summary>
public sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("languages", "site", t =>
        {
            t.HasCheckConstraint("ck_languages_status", "status IN ('open','coming_soon','hidden')");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Code).HasMaxLength(32).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(60).IsRequired();
        builder.Property(l => l.NativeName).HasMaxLength(60).IsRequired();
        builder.Property(l => l.Tagline).HasMaxLength(160).HasDefaultValue("").IsRequired();
        builder.Property(l => l.DescriptionMarkdown).HasDefaultValue("").IsRequired();

        // Enum C# <-> chuỗi lưu DB (open|coming_soon|hidden) — nguồn ánh xạ DUY NHẤT LanguageStatuses (Domain, §5.1.2).
        builder.Property(l => l.Status)
            .HasConversion(v => LanguageStatuses.ToCode(v), v => LanguageStatuses.Parse(v))
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(l => l.AppUrl).HasMaxLength(300);
        builder.Property(l => l.AccentColor).HasMaxLength(9);
        // cover_media_id CHƯA có FK ở W3a (bảng site.media_files tạo ở W4) — chỉ cột trần.
        builder.Property(l => l.CoverMediaId);
        builder.Property(l => l.SortOrder).IsRequired();
        builder.Property(l => l.UpdatedAt).IsRequired();
        builder.Property(l => l.UpdatedBy);

        // Concurrency token ánh xạ cột hệ thống xmin (Npgsql) — KHÔNG tạo cột thật (§5.1.2, R-CA3).
        builder.Property(l => l.Version).IsRowVersion();

        builder.HasIndex(l => l.Code).IsUnique().HasDatabaseName("ux_languages_code");
        builder.HasIndex(l => l.SortOrder).HasDatabaseName("ix_languages_sort_order");
    }
}
