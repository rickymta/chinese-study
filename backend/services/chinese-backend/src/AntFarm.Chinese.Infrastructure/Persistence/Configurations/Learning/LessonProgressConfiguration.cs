using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Lessons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Learning;

/// <summary>Bảng learning.lesson_progress (§5.1.1, migration F9_Lessons) — khoá chính ghép (UserId, LessonId).</summary>
public sealed class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> builder)
    {
        builder.ToTable("lesson_progress", "learning", t =>
        {
            t.HasCheckConstraint("ck_lesson_progress_status", "status IN ('in_progress','completed')");
            t.HasCheckConstraint("ck_lesson_progress_best_score", "best_score_percent IS NULL OR best_score_percent BETWEEN 0 AND 100");
        });

        builder.HasKey(p => new { p.UserId, p.LessonId });

        builder.Property(p => p.Status).HasMaxLength(16).IsRequired();
        builder.Property(p => p.StartedAt).IsRequired();
        builder.Property(p => p.CompletedAt);
        builder.Property(p => p.BestScorePercent).HasColumnType("smallint");
        builder.Property(p => p.AttemptsCount).HasDefaultValue(0).IsRequired();
        builder.Property(p => p.LastAttemptAt);
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Lesson>().WithMany().HasForeignKey(p => p.LessonId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.UserId, p.Status }).HasDatabaseName("ix_lesson_progress_user_status");
    }
}
