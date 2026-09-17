using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Learning;

/// <summary>Bảng learning.learner_settings (§5.1.2, migration F7_Srs) — PK = UserId (một người học một bộ cài đặt); không có dòng ⇒ áp <see cref="LearnerSettings.Defaults"/> ở tầng Application.</summary>
public sealed class LearnerSettingsConfiguration : IEntityTypeConfiguration<LearnerSettings>
{
    public void Configure(EntityTypeBuilder<LearnerSettings> builder)
    {
        builder.ToTable("learner_settings", "learning", t =>
        {
            t.HasCheckConstraint("ck_learner_settings_daily_new_cards", "daily_new_cards BETWEEN 0 AND 50");
            t.HasCheckConstraint("ck_learner_settings_daily_review_limit", "daily_review_limit BETWEEN 10 AND 1000");
            t.HasCheckConstraint("ck_learner_settings_desired_retention", "desired_retention BETWEEN 0.80 AND 0.97");
            t.HasCheckConstraint("ck_learner_settings_tts_rate", "tts_rate BETWEEN 0.50 AND 1.20");
        });

        builder.HasKey(s => s.UserId);
        builder.Property(s => s.UserId).ValueGeneratedNever();

        // DEFAULT = đúng LearnerSettings.Defaults (§5.1.2, review F7.1) — CHỈ để khớp thiết kế DB
        // (đọc SQL trực tiếp thấy đúng ý định); Application LUÔN gửi giá trị tường minh lúc PUT nên
        // KHÔNG được để EF tự suy "store-generated". ⚠️ `HasDefaultValue` MẶC ĐỊNH kéo theo
        // `ValueGenerated.OnAdd` — EF coi cột đó như identity: nếu giá trị C# đang TRÙNG giá trị mặc
        // định CLR (0/false) thì EF BỎ HẲN cột khỏi câu INSERT (để DB tự áp DEFAULT) rồi đọc NGƯỢC
        // giá trị đó vào entity sau khi lưu — `dailyNewCards = 0` (một giá trị HỢP LỆ, 0..50) bị
        // lặng lẽ ghi đè thành 10 theo cách này (bắt được bằng test `DailyReviewLimit10_15TheDenHan...`,
        // review F7.1 lần 2). `ValueGeneratedNever()` sau `HasDefaultValue` buộc EF luôn gửi đúng
        // giá trị trong bộ nhớ, bất kể có trùng CLR-default hay không.
        builder.Property(s => s.DailyNewCards).HasDefaultValue(LearnerSettings.Defaults.DailyNewCards).ValueGeneratedNever().IsRequired();
        builder.Property(s => s.DailyReviewLimit).HasDefaultValue(LearnerSettings.Defaults.DailyReviewLimit).ValueGeneratedNever().IsRequired();
        builder.Property(s => s.DesiredRetention).HasColumnType("numeric(3,2)").HasDefaultValue(LearnerSettings.Defaults.DesiredRetention).ValueGeneratedNever().IsRequired();
        builder.Property(s => s.TtsRate).HasColumnType("numeric(3,2)").HasDefaultValue(LearnerSettings.Defaults.TtsRate).ValueGeneratedNever().IsRequired();
        builder.Property(s => s.AutoPlayAudio).HasDefaultValue(LearnerSettings.Defaults.AutoPlayAudio).ValueGeneratedNever().IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        // PK = FK (một-một thật sự) — người dùng bị xoá thì cài đặt học tập của họ xoá theo.
        builder.HasOne<User>().WithOne().HasForeignKey<LearnerSettings>(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
