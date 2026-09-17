using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Learning;

/// <summary>Bảng learning.character_writing_stats (§5.1.2, migration F8_Writing) — khoá chính ghép (UserId, Hanzi), một dòng mỗi cặp (người dùng, chữ) đã từng luyện viết.</summary>
public sealed class CharacterWritingStatsConfiguration : IEntityTypeConfiguration<CharacterWritingStats>
{
    public void Configure(EntityTypeBuilder<CharacterWritingStats> builder)
    {
        builder.ToTable("character_writing_stats", "learning", t =>
        {
            t.HasCheckConstraint("ck_char_writing_stats_last_mode", "last_mode IN ('guided','recall')");
        });

        builder.HasKey(s => new { s.UserId, s.Hanzi });

        builder.Property(s => s.Hanzi).HasMaxLength(4).IsRequired();
        builder.Property(s => s.Attempts).HasDefaultValue(0).IsRequired();
        builder.Property(s => s.GuidedAttempts).HasDefaultValue(0).IsRequired();
        builder.Property(s => s.RecallAttempts).HasDefaultValue(0).IsRequired();
        builder.Property(s => s.LastMode).HasMaxLength(8).IsRequired();
        builder.Property(s => s.LastMistakes).HasColumnType("smallint").IsRequired();
        builder.Property(s => s.LastHints).HasColumnType("smallint").IsRequired();
        builder.Property(s => s.BestRecallMistakes).HasColumnType("smallint");
        builder.Property(s => s.CleanRecallDays).HasColumnType("smallint").HasDefaultValue((short)0).IsRequired();
        builder.Property(s => s.LastCleanRecallDate);
        builder.Property(s => s.FirstPracticedAt).IsRequired();
        builder.Property(s => s.LastPracticedAt).IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Character>().WithMany().HasForeignKey(s => s.Hanzi).HasPrincipalKey(c => c.Hanzi).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.UserId, s.LastPracticedAt }).HasDatabaseName("ix_char_writing_stats_user_last");
    }
}
