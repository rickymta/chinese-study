using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Lessons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Learning;

/// <summary>Bảng learning.quiz_attempts (§5.1.1, migration F9_Lessons) — idempotent theo ClientAttemptId (R-LS9).</summary>
public sealed class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.ToTable("quiz_attempts", "learning", t =>
        {
            t.HasCheckConstraint("ck_quiz_attempts_total", "total > 0");
            t.HasCheckConstraint("ck_quiz_attempts_correct", "correct BETWEEN 0 AND total");
            t.HasCheckConstraint("ck_quiz_attempts_score_percent", "score_percent BETWEEN 0 AND 100");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ClientAttemptId).IsRequired();
        builder.Property(a => a.StartedAt);
        builder.Property(a => a.SubmittedAt).IsRequired();
        builder.Property(a => a.DurationMs);
        builder.Property(a => a.Total).HasColumnType("smallint").IsRequired();
        builder.Property(a => a.Correct).HasColumnType("smallint").IsRequired();
        builder.Property(a => a.ScorePercent).HasColumnType("smallint").IsRequired();
        builder.Property(a => a.Passed).IsRequired();

        // jsonb — ảnh chụp R-LS11 (Application/LessonJson), KHÔNG owned/JsonDocument (§5.1).
        builder.Property(a => a.Answers).HasColumnType("jsonb").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        // R-CA7: bài có lần làm KHÔNG xoá cứng được (RESTRICT) — chỉ chuyển archived.
        builder.HasOne<Lesson>().WithMany().HasForeignKey(a => a.LessonId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.ClientAttemptId).IsUnique().HasDatabaseName("ux_quiz_attempts_client_attempt_id");
        builder.HasIndex(a => new { a.UserId, a.LessonId, a.SubmittedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("ix_quiz_attempts_user_lesson");
    }
}
