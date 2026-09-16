using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Domain.Pinyin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Learning;

/// <summary>Bảng learning.tone_drill_sessions (§5.1.1, migration F5_ToneDrill) — một dòng mỗi phiên luyện thanh đã nộp.</summary>
public sealed class ToneDrillSessionConfiguration : IEntityTypeConfiguration<ToneDrillSession>
{
    public void Configure(EntityTypeBuilder<ToneDrillSession> builder)
    {
        builder.ToTable("tone_drill_sessions", "learning", t =>
        {
            t.HasCheckConstraint("ck_tone_drill_sessions_mode", "mode IN ('listen_tone','tone_pair')");
            t.HasCheckConstraint("ck_tone_drill_sessions_finished_after_started", "finished_at >= started_at");
            t.HasCheckConstraint("ck_tone_drill_sessions_total", "total BETWEEN 1 AND 100");
            t.HasCheckConstraint("ck_tone_drill_sessions_correct", "correct BETWEEN 0 AND total");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ClientSessionId).IsRequired();

        // Lưu chuỗi snake_case (không phải số) — đọc trực tiếp bằng SQL cũng hiểu ngay, khớp giá
        // trị JSON của API (JsonStringEnumConverter(SnakeCaseLower) đăng ký chung ở Program.cs).
        builder.Property(s => s.Mode)
            .HasConversion(
                mode => mode == ToneDrillMode.ListenTone ? "listen_tone" : "tone_pair",
                value => value == "listen_tone" ? ToneDrillMode.ListenTone : ToneDrillMode.TonePair)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(s => s.StartedAt).IsRequired();
        builder.Property(s => s.FinishedAt).IsRequired();
        builder.Property(s => s.Total).IsRequired();
        builder.Property(s => s.Correct).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);

        // R5-10: idempotent theo (userId, clientSessionId) — nộp lại cùng id không nhân đôi.
        // Đặt tên tường minh để ToneDrillService dò chính xác lỗi 23505 khi hai request đua nhau
        // cùng nộp một clientSessionId (thay vì đoán tên do EF tự sinh).
        builder.HasIndex(s => new { s.UserId, s.ClientSessionId })
            .IsUnique()
            .HasDatabaseName("ux_tone_drill_sessions_user_id_client_session_id");
        builder.HasIndex(s => new { s.UserId, s.FinishedAt }).IsDescending(false, true);

        builder.HasMany(s => s.Answers)
            .WithOne()
            .HasForeignKey(a => a.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Answers là IReadOnlyList backed bởi field riêng (_answers) — EF ghi thẳng vào field,
        // KHÔNG qua property (property không có setter công khai, đúng ý đồ bất biến của entity).
        builder.Navigation(s => s.Answers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
