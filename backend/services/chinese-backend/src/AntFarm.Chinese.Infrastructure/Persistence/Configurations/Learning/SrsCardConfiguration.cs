using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Domain.Srs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Learning;

/// <summary>Bảng learning.srs_cards (§5.1.2, migration F7_Srs) — một dòng mỗi thẻ ôn tập FSRS-6 của một người học.</summary>
public sealed class SrsCardConfiguration : IEntityTypeConfiguration<SrsCard>
{
    public void Configure(EntityTypeBuilder<SrsCard> builder)
    {
        builder.ToTable("srs_cards", "learning", t =>
        {
            t.HasCheckConstraint("ck_srs_cards_card_type", "card_type IN ('hanzi_to_meaning')");
            t.HasCheckConstraint("ck_srs_cards_state", "state IN ('new','learning','review','relearning')");
            t.HasCheckConstraint("ck_srs_cards_source", "source IN ('path','manual','lesson')");
        });

        builder.HasKey(c => c.Id);

        // DEFAULT (§5.1.2, review F7.1) — cho phép InsertNewCardIfMissingAsync (SrsCardService) chèn
        // bằng SQL thẳng KHÔNG cần liệt kê 3 cột này (card_type luôn 'hanzi_to_meaning' ở F7;
        // reps/lapses luôn bắt đầu 0). ValueGeneratedNever() sau HasDefaultValue: PHÒNG THỦ cho
        // đường ghi qua EF (Add()+SaveChanges, hiện KHÔNG dùng để tạo thẻ nhưng có thể có sau) —
        // nếu không có dòng này, HasDefaultValue kéo theo ValueGenerated.OnAdd khiến EF BỎ cột khỏi
        // INSERT khi giá trị C# trùng CLR-default (0) rồi đọc ngược giá trị DB vào entity, y hệt bug
        // đã bắt được ở learner_settings.daily_new_cards=0 (xem LearnerSettingsConfiguration).
        builder.Property(c => c.CardType).HasMaxLength(24).HasDefaultValue(SrsCardTypes.HanziToMeaning).ValueGeneratedNever().IsRequired();

        // Lưu chuỗi snake_case (không phải số) — đọc trực tiếp bằng SQL cũng hiểu ngay (cùng quy
        // ước ToneDrillSession.Mode, F5) — dùng chung SrsStateCodes với SrsReviewLogConfiguration.
        builder.Property(c => c.State)
            .HasConversion(v => SrsStateCodes.ToCode(v), v => SrsStateCodes.FromCode(v))
            .HasMaxLength(12)
            .IsRequired();

        // smallint (không phải integer mặc định của EF cho int?) — step chỉ 0/1 trong đời thẻ
        // (learning_steps/relearning_steps mặc định tối đa 2 phần tử, §5.2.6) — review F7.1.
        builder.Property(c => c.Step).HasColumnType("smallint");
        builder.Property(c => c.DueAt).IsRequired();
        builder.Property(c => c.Stability);
        builder.Property(c => c.Difficulty);
        builder.Property(c => c.Reps).HasDefaultValue(0).ValueGeneratedNever().IsRequired();
        builder.Property(c => c.Lapses).HasDefaultValue(0).ValueGeneratedNever().IsRequired();
        builder.Property(c => c.LastReviewAt);
        builder.Property(c => c.FirstReviewedAt);
        builder.Property(c => c.FirstReviewedLocalDate);
        builder.Property(c => c.IsSuspended).IsRequired();
        builder.Property(c => c.Source).HasMaxLength(12).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Word>().WithMany().HasForeignKey(c => c.WordId).OnDelete(DeleteBehavior.Cascade);

        // R7-2: một từ = một thẻ hanzi_to_meaning/người học.
        builder.HasIndex(c => new { c.UserId, c.WordId, c.CardType })
            .IsUnique()
            .HasDatabaseName("ux_srs_cards_user_word_type");

        // Chỉ mục MỘT PHẦN (§5.1.2 "Lệch hợp đồng gốc") — hàng đợi (R7-7) chỉ bao giờ đọc thẻ
        // CHƯA tạm dừng, lọc bớt để chỉ mục nhỏ và nhanh hơn is_suspended=true không cần tới.
        builder.HasIndex(c => new { c.UserId, c.State, c.DueAt })
            .HasDatabaseName("ix_srs_cards_user_state_due")
            .HasFilter("is_suspended = false");

        builder.HasIndex(c => new { c.UserId, c.FirstReviewedLocalDate }).HasDatabaseName("ix_srs_cards_user_first_date");
    }
}
