using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Learning;

/// <summary>
/// Bảng learning.writing_attempts (§5.1.2, migration F8_Writing) — idempotent theo
/// <c>ClientAttemptId</c> (R-W8). FK <c>hanzi</c> trỏ khoá THAY THẾ <c>content.characters.hanzi</c>
/// (<see cref="CharacterConfiguration"/> đã có <c>UNIQUE INDEX ux_characters_hanzi</c> từ F6 — dùng
/// <c>HasPrincipalKey</c> thay vì thêm FK tới <c>Id</c> vì bảng này chỉ biết mặt chữ, không có
/// <c>character_id</c>). Postgres CHO PHÉP FK trỏ cột có unique index dù không khai
/// <c>ADD CONSTRAINT UNIQUE</c> tường minh (đã kiểm chứng thủ công ngày 17/09/2026) — EF vẫn cần
/// <c>HasPrincipalKey</c> để biết đây là khoá thay thế hợp lệ; đã soát <c>dotnet ef migrations
/// script</c> để chắc KHÔNG sinh thêm chỉ mục/ràng buộc trùng với <c>ux_characters_hanzi</c>.
/// </summary>
public sealed class WritingAttemptConfiguration : IEntityTypeConfiguration<WritingAttempt>
{
    public void Configure(EntityTypeBuilder<WritingAttempt> builder)
    {
        builder.ToTable("writing_attempts", "learning", t =>
        {
            t.HasCheckConstraint("ck_writing_attempts_mode", "mode IN ('guided','recall')");
            t.HasCheckConstraint("ck_writing_attempts_total_strokes", "total_strokes BETWEEN 1 AND 64");
            t.HasCheckConstraint("ck_writing_attempts_total_mistakes", "total_mistakes BETWEEN 0 AND 500");
            t.HasCheckConstraint("ck_writing_attempts_hints_used", "hints_used BETWEEN 0 AND 200");
            t.HasCheckConstraint("ck_writing_attempts_duration_ms", "duration_ms IS NULL OR duration_ms BETWEEN 0 AND 3600000");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ClientAttemptId).IsRequired();
        builder.Property(a => a.Hanzi).HasMaxLength(4).IsRequired();
        builder.Property(a => a.Mode).HasMaxLength(8).IsRequired();
        builder.Property(a => a.TotalStrokes).HasColumnType("smallint").IsRequired();
        builder.Property(a => a.TotalMistakes).HasColumnType("smallint").IsRequired();
        builder.Property(a => a.HintsUsed).HasColumnType("smallint").IsRequired();
        builder.Property(a => a.DurationMs);
        builder.Property(a => a.IsClean).IsRequired();
        builder.Property(a => a.CompletedAt).IsRequired();
        builder.Property(a => a.LocalDate).IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        // R-W7: chữ không có trong content.characters ⇒ 422 UNKNOWN_CHARACTER ở tầng Application
        // TRƯỚC khi ghi — RESTRICT chỉ là lưới an toàn tầng DB, không phải đường lỗi chính.
        builder.HasOne<Character>().WithMany().HasForeignKey(a => a.Hanzi).HasPrincipalKey(c => c.Hanzi).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.ClientAttemptId).IsUnique().HasDatabaseName("ux_writing_attempts_client_attempt_id");
        builder.HasIndex(a => new { a.UserId, a.Hanzi, a.CompletedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("ix_writing_attempts_user_hanzi");
        builder.HasIndex(a => new { a.UserId, a.CompletedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_writing_attempts_user_completed");
    }
}
