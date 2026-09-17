using AntFarm.Chinese.Domain.Srs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Learning;

/// <summary>
/// Bảng learning.srs_review_logs (§5.1.2, migration F7_Srs) — MỘT DÒNG MỖI LƯỢT CHẤM, idempotent
/// theo <c>client_review_id</c> (R7-8, tên chỉ mục dùng ở <c>SrsReviewService</c> để dò lỗi 23505).
/// </summary>
public sealed class SrsReviewLogConfiguration : IEntityTypeConfiguration<SrsReviewLog>
{
    public void Configure(EntityTypeBuilder<SrsReviewLog> builder)
    {
        builder.ToTable("srs_review_logs", "learning", t =>
        {
            t.HasCheckConstraint("ck_srs_review_logs_rating", "rating BETWEEN 1 AND 4");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ClientReviewId).IsRequired();

        // Giá trị số trùng py-fsrs Rating (1..4, §5.2.6) — lưu smallint, KHÔNG chuỗi (khác State,
        // không cần đọc trực tiếp bằng mắt thường xuyên bằng SQL như trạng thái thẻ).
        builder.Property(l => l.Rating)
            .HasConversion(r => (short)r, v => (SrsRating)v)
            .IsRequired();

        builder.Property(l => l.ReviewedAt).IsRequired();
        builder.Property(l => l.LocalDate).IsRequired();

        builder.Property(l => l.StateBefore)
            .HasConversion(v => SrsStateCodes.ToCode(v), v => SrsStateCodes.FromCode(v))
            .HasMaxLength(12)
            .IsRequired();
        builder.Property(l => l.StepBefore).HasColumnType("smallint"); // giống srs_cards.step (review F7.1) — chỉ 0/1
        builder.Property(l => l.StabilityBefore);
        builder.Property(l => l.DifficultyBefore);

        builder.Property(l => l.StateAfter)
            .HasConversion(v => SrsStateCodes.ToCode(v), v => SrsStateCodes.FromCode(v))
            .HasMaxLength(12)
            .IsRequired();
        builder.Property(l => l.StabilityAfter).IsRequired();
        builder.Property(l => l.DifficultyAfter).IsRequired();
        builder.Property(l => l.DueAfter).IsRequired();

        builder.Property(l => l.ElapsedDays).IsRequired();
        builder.Property(l => l.ScheduledDays).IsRequired();
        builder.Property(l => l.DurationMs);

        // card_id CASCADE theo thẻ (thẻ lại CASCADE theo user) — user_id KHÔNG khai FK (§5.1.2:
        // "không FK vì thẻ đã CASCADE theo user") để tránh hai đường cascade cùng gốc access.users.
        builder.HasOne<SrsCard>().WithMany().HasForeignKey(l => l.CardId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => l.ClientReviewId).IsUnique().HasDatabaseName("ux_srs_review_logs_client_review_id");
        builder.HasIndex(l => new { l.UserId, l.LocalDate }).HasDatabaseName("ix_srs_review_logs_user_date");
        builder.HasIndex(l => new { l.CardId, l.ReviewedAt }).HasDatabaseName("ix_srs_review_logs_card");
    }
}
