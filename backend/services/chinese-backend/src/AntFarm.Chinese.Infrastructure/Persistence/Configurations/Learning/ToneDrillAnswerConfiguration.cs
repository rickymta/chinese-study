using AntFarm.Chinese.Domain.Pinyin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Learning;

/// <summary>
/// Bảng learning.tone_drill_answers (§5.1.1, migration F5_ToneDrill) — MỘT DÒNG MỖI PHẦN.
/// <see cref="ToneDrillAnswer.UserId"/> phi chuẩn hoá (không FK) để <c>ToneStatsService</c> lọc
/// theo (user, thanh) không cần join qua session.
/// </summary>
public sealed class ToneDrillAnswerConfiguration : IEntityTypeConfiguration<ToneDrillAnswer>
{
    public void Configure(EntityTypeBuilder<ToneDrillAnswer> builder)
    {
        builder.ToTable("tone_drill_answers", "learning", t =>
        {
            t.HasCheckConstraint("ck_tone_drill_answers_item_index", "item_index >= 0");
            t.HasCheckConstraint("ck_tone_drill_answers_part_index", "part_index IN (0,1)");
            t.HasCheckConstraint("ck_tone_drill_answers_expected_tone", "expected_tone BETWEEN 1 AND 4");
            t.HasCheckConstraint("ck_tone_drill_answers_answered_tone", "answered_tone BETWEEN 1 AND 4");
            t.HasCheckConstraint("ck_tone_drill_answers_response_ms", "response_ms IS NULL OR response_ms BETWEEN 0 AND 600000");
            t.HasCheckConstraint("ck_tone_drill_answers_replay_count", "replay_count BETWEEN 0 AND 100");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Syllable).HasMaxLength(8).IsRequired();
        builder.Property(a => a.Hanzi).HasMaxLength(4).IsRequired();
        builder.Property(a => a.ExpectedTone).IsRequired();
        builder.Property(a => a.AnsweredTone).IsRequired();
        builder.Property(a => a.IsCorrect).IsRequired();
        builder.Property(a => a.ReplayCount).IsRequired().HasDefaultValue((short)0);
        builder.Property(a => a.AnsweredAt).IsRequired();

        // (session_id, item_index, part_index) là khoá tự nhiên của MỘT PHẦN — chống ghi trùng phần.
        builder.HasIndex(a => new { a.SessionId, a.ItemIndex, a.PartIndex }).IsUnique();

        // Cửa sổ thống kê R5-13: 200 phần gần nhất mỗi thanh, sắp answered_at DESC, part_index DESC.
        builder.HasIndex(a => new { a.UserId, a.ExpectedTone, a.AnsweredAt, a.PartIndex })
            .IsDescending(false, false, true, true);
    }
}
