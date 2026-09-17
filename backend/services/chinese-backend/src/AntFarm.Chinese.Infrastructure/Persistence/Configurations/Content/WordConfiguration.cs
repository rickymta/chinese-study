using AntFarm.Chinese.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Content;

/// <summary>Bảng content.words (§5.1.1, migration F6_Vocabulary).</summary>
public sealed class WordConfiguration : IEntityTypeConfiguration<Word>
{
    public void Configure(EntityTypeBuilder<Word> builder)
    {
        builder.ToTable("words", "content", t =>
        {
            t.HasCheckConstraint("ck_words_hsk3_level", "hsk3_level IS NULL OR hsk3_level BETWEEN 1 AND 7");
            t.HasCheckConstraint("ck_words_hsk2_level", "hsk2_level IS NULL OR hsk2_level BETWEEN 1 AND 6");
            t.HasCheckConstraint("ck_words_hsk_exam2026_level", "hsk_exam2026_level IS NULL OR hsk_exam2026_level BETWEEN 1 AND 7");
            t.HasCheckConstraint("ck_words_meaning_vi_status", "meaning_vi_status IN ('machine','reviewed')");
            t.HasCheckConstraint("ck_words_meaning_vi_source", "meaning_vi_source IN ('cvdict','machine','manual')");
            t.HasCheckConstraint("ck_words_han_viet_status", "han_viet_status IS NULL OR han_viet_status IN ('derived','reviewed')");
        });

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Simplified).HasMaxLength(32).IsRequired();
        builder.Property(w => w.Traditional).HasMaxLength(32);
        builder.Property(w => w.Variants).IsRequired();
        builder.Property(w => w.Pinyin).HasMaxLength(128).IsRequired();
        builder.Property(w => w.PinyinCompact).HasMaxLength(128).IsRequired();
        builder.Property(w => w.PinyinSearch).HasMaxLength(128).IsRequired();

        // UseSnakeCaseNamingConvention KHÔNG chèn '_' giữa chữ số và chữ hoa liền sau (vd
        // "Hsk3Level" → "hsk3level" thay vì "hsk3_level") — đặt tên cột TƯỜNG MINH để khớp đúng
        // §5.1.1 (phát hiện lúc kiểm migration sinh ra, review F6.2).
        builder.Property(w => w.Hsk3Level).HasColumnName("hsk3_level");
        builder.Property(w => w.Hsk2Level).HasColumnName("hsk2_level");
        builder.Property(w => w.HskExam2026Level).HasColumnName("hsk_exam2026_level");

        builder.Property(w => w.Pos).IsRequired();
        builder.Property(w => w.UsageNote).HasMaxLength(64);
        builder.Property(w => w.MeaningsEn).IsRequired();
        builder.Property(w => w.MeaningsVi).IsRequired();
        builder.Property(w => w.MeaningViStatus).HasMaxLength(16).IsRequired();
        builder.Property(w => w.MeaningViSource).HasMaxLength(16).IsRequired();
        builder.Property(w => w.HanViet).HasMaxLength(64);
        builder.Property(w => w.HanVietPlain).HasMaxLength(64);
        builder.Property(w => w.HanVietStatus).HasMaxLength(16);
        builder.Property(w => w.SearchVi).IsRequired();
        builder.Property(w => w.SearchViPlain).IsRequired();
        builder.Property(w => w.Sources).IsRequired();
        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.UpdatedAt).IsRequired();

        // Concurrency token ánh xạ cột hệ thống xmin (Npgsql) — KHÔNG tạo cột thật (§5.1, F10 R-CA3).
        builder.Property(w => w.Version).IsRowVersion();

        builder.HasIndex(w => new { w.Simplified, w.Pinyin }).IsUnique().HasDatabaseName("ux_words_simplified_pinyin");

        // varchar_pattern_ops: cần cho LIKE 'x%' khi collation DB không phải 'C' (§5.1.1).
        builder.HasIndex(w => w.Simplified).HasDatabaseName("ix_words_simplified_pattern").HasOperators("varchar_pattern_ops");
        builder.HasIndex(w => w.Traditional).HasDatabaseName("ix_words_traditional");
        builder.HasIndex(w => w.PinyinCompact).HasDatabaseName("ix_words_pinyin_compact").HasOperators("varchar_pattern_ops");
        builder.HasIndex(w => w.PinyinSearch).HasDatabaseName("ix_words_pinyin_search").HasOperators("varchar_pattern_ops");
        builder.HasIndex(w => w.HanVietPlain).HasDatabaseName("ix_words_han_viet_plain");

        // KHÔNG unique: path_order có thể đổi thứ tự giữa các lượt nạp (§5.1.1).
        builder.HasIndex(w => new { w.Hsk3Level, w.PathOrder }).HasDatabaseName("ix_words_level_path");
        builder.HasIndex(w => w.Hsk2Level).HasDatabaseName("ix_words_hsk2");

        builder.HasIndex(w => w.SearchVi).HasDatabaseName("ix_words_search_vi_trgm").HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(w => w.SearchViPlain).HasDatabaseName("ix_words_search_vi_plain_trgm").HasMethod("gin").HasOperators("gin_trgm_ops");

        // F10 (§5.1.3, R-CA11): màn duyệt nghĩa lọc/sắp theo đúng thứ tự ba cột này (path_order tăng dần
        // trong CÙNG cấp/trạng thái — từ sắp học lên trước, RK4).
        builder.HasIndex(w => new { w.MeaningViStatus, w.Hsk3Level, w.PathOrder }).HasDatabaseName("ix_words_review");
    }
}
