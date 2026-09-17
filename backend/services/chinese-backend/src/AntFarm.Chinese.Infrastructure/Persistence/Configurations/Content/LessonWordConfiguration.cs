using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Domain.Lessons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Content;

/// <summary>Bảng content.lesson_words (§5.1.1, migration F9_Lessons) — khoá chính ghép (LessonId, WordId).</summary>
public sealed class LessonWordConfiguration : IEntityTypeConfiguration<LessonWord>
{
    public void Configure(EntityTypeBuilder<LessonWord> builder)
    {
        builder.ToTable("lesson_words", "content");

        builder.HasKey(w => new { w.LessonId, w.WordId });

        builder.Property(w => w.OrderIndex).HasColumnType("smallint").IsRequired();

        builder.HasOne<Lesson>().WithMany().HasForeignKey(w => w.LessonId).OnDelete(DeleteBehavior.Cascade);
        // R-LS14/R-CA7: từ đã dùng trong bài KHÔNG bị xoá cứng khỏi content.words vì thao tác khác (RESTRICT).
        builder.HasOne<Word>().WithMany().HasForeignKey(w => w.WordId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => w.WordId).HasDatabaseName("ix_lesson_words_word");
    }
}
