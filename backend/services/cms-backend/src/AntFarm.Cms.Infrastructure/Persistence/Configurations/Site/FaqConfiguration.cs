using AntFarm.Cms.Domain.Site;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Cms.Infrastructure.Persistence.Configurations.Site;

/// <summary>Bảng site.faqs (§5.1.2, migration W3b_Faqs).</summary>
public sealed class FaqConfiguration : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> builder)
    {
        builder.ToTable("faqs", "site");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Question).HasMaxLength(300).IsRequired();
        builder.Property(f => f.AnswerMarkdown).IsRequired();
        builder.Property(f => f.GroupKey).HasMaxLength(32).HasDefaultValue("general").IsRequired();
        builder.Property(f => f.SortOrder).IsRequired();
        builder.Property(f => f.IsPublished).IsRequired();
        builder.Property(f => f.UpdatedAt).IsRequired();
        builder.Property(f => f.UpdatedBy);

        // Concurrency token ánh xạ cột hệ thống xmin (Npgsql) — KHÔNG tạo cột thật (§5.1.2, R-CA3).
        builder.Property(f => f.Version).IsRowVersion();

        builder.HasIndex(f => new { f.IsPublished, f.GroupKey, f.SortOrder }).HasDatabaseName("ix_faqs_published_group_sort");
    }
}
