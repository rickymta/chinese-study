using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Domain.Lessons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Content;

/// <summary>Bảng content.lessons (§5.1.1, migration F9_Lessons).</summary>
public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons", "content", t =>
        {
            t.HasCheckConstraint("ck_lessons_level", "level IN ('hsk1')");
            t.HasCheckConstraint("ck_lessons_estimated_minutes", "estimated_minutes BETWEEN 1 AND 120");
            t.HasCheckConstraint("ck_lessons_status", "status IN ('draft','published','archived')");
            t.HasCheckConstraint("ck_lessons_review_status", "review_status IN ('machine','reviewed')");
            t.HasCheckConstraint("ck_lessons_source", "source IN ('seed','admin')");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Slug).HasMaxLength(64).IsRequired();
        builder.Property(l => l.Title).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Topic).HasMaxLength(64).IsRequired();
        builder.Property(l => l.Level).HasMaxLength(16).HasDefaultValue("hsk1").IsRequired();
        builder.Property(l => l.OrderIndex).IsRequired();
        builder.Property(l => l.Summary).HasMaxLength(1000).HasDefaultValue("").IsRequired();
        builder.Property(l => l.Objectives).HasDefaultValueSql("'{}'").IsRequired();
        builder.Property(l => l.EstimatedMinutes).HasDefaultValue((short)15).IsRequired();

        // jsonb — mảng GlossaryItem đã tuần tự hoá (Application/LessonJson), KHÔNG owned/JsonDocument (§5.1).
        builder.Property(l => l.Glossary).HasColumnType("jsonb").HasDefaultValue("[]").IsRequired();

        builder.Property(l => l.Status).HasMaxLength(16).IsRequired();
        builder.Property(l => l.ReviewStatus).HasMaxLength(16).HasDefaultValue(LessonReviewStatuses.Machine).IsRequired();
        builder.Property(l => l.Source).HasMaxLength(8).IsRequired();
        builder.Property(l => l.SourceHash).HasMaxLength(64).IsFixedLength();
        builder.Property(l => l.PublishedAt);
        builder.Property(l => l.ReviewedAt);
        builder.Property(l => l.ReviewedBy);
        builder.Property(l => l.CreatedBy);
        builder.Property(l => l.EditedAt);
        builder.Property(l => l.EditedBy);
        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.UpdatedAt).IsRequired();

        // Concurrency token ánh xạ cột hệ thống xmin (Npgsql) — KHÔNG tạo cột thật (§5.1, F10 R-CA3).
        builder.Property(l => l.Version).IsRowVersion();

        builder.HasOne<User>().WithMany().HasForeignKey(l => l.ReviewedBy).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(l => l.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(l => l.EditedBy).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => l.Slug).IsUnique().HasDatabaseName("ux_lessons_slug");
        builder.HasIndex(l => new { l.Status, l.OrderIndex }).HasDatabaseName("ix_lessons_status_order");
    }
}
