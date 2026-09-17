using AntFarm.Chinese.Domain.Lessons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Content;

/// <summary>Bảng content.quiz_questions (§5.1.1, migration F9_Lessons).</summary>
public sealed class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{
    public void Configure(EntityTypeBuilder<QuizQuestion> builder)
    {
        builder.ToTable("quiz_questions", "content", t =>
        {
            t.HasCheckConstraint("ck_quiz_questions_type", "type IN ('single_choice','listen_choice')");
            t.HasCheckConstraint("ck_quiz_questions_prompt_lang", "prompt_lang IN ('vi','zh')");
        });

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Key).HasMaxLength(32).IsRequired();
        builder.Property(q => q.OrderIndex).HasColumnType("smallint").IsRequired();
        builder.Property(q => q.Type).HasMaxLength(16).IsRequired();
        builder.Property(q => q.Prompt).HasMaxLength(300).IsRequired();
        builder.Property(q => q.PromptLang).HasMaxLength(8).IsRequired();
        builder.Property(q => q.PromptPinyin).HasMaxLength(300);
        builder.Property(q => q.AudioText).HasMaxLength(100);

        // jsonb — mảng QuizOption đã tuần tự hoá (Application/LessonJson), KHÔNG owned/JsonDocument (§5.1).
        builder.Property(q => q.Options).HasColumnType("jsonb").IsRequired();

        builder.Property(q => q.CorrectOptionId).HasMaxLength(1).IsRequired();
        builder.Property(q => q.Explanation).HasMaxLength(500).HasDefaultValue("").IsRequired();

        builder.HasOne<Lesson>().WithMany().HasForeignKey(q => q.LessonId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(q => new { q.LessonId, q.Key }).IsUnique().HasDatabaseName("ux_quiz_questions_lesson_key");
        builder.HasIndex(q => new { q.LessonId, q.OrderIndex }).HasDatabaseName("ix_quiz_questions_lesson");
    }
}
