using AntFarm.Chinese.Domain.Lessons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Content;

/// <summary>Bảng content.lesson_blocks (§5.1.1, migration F9_Lessons).</summary>
public sealed class LessonBlockConfiguration : IEntityTypeConfiguration<LessonBlock>
{
    public void Configure(EntityTypeBuilder<LessonBlock> builder)
    {
        builder.ToTable("lesson_blocks", "content", t =>
            t.HasCheckConstraint("ck_lesson_blocks_type", "type IN ('text','dialogue','grammar','tip')"));

        builder.HasKey(b => b.Id);

        builder.Property(b => b.OrderIndex).HasColumnType("smallint").IsRequired();
        builder.Property(b => b.Type).HasMaxLength(16).IsRequired();

        // jsonb — hình dạng tuỳ Type (Domain.Lessons.Payloads), KHÔNG owned/JsonDocument (§5.1).
        builder.Property(b => b.Payload).HasColumnType("jsonb").IsRequired();

        builder.HasOne<Lesson>().WithMany().HasForeignKey(b => b.LessonId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.LessonId, b.OrderIndex }).HasDatabaseName("ix_lesson_blocks_lesson");
    }
}
