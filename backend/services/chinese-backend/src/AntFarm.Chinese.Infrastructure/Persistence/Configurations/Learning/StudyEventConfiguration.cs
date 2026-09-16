using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Learning;

/// <summary>Bảng learning.study_events (§5.1.1, migration F5_ToneDrill) — sổ hoạt động học dùng chung mọi loại bài tập.</summary>
public sealed class StudyEventConfiguration : IEntityTypeConfiguration<StudyEvent>
{
    public void Configure(EntityTypeBuilder<StudyEvent> builder)
    {
        builder.ToTable("study_events", "learning", t =>
        {
            t.HasCheckConstraint("ck_study_events_quantity", "quantity >= 0");
            t.HasCheckConstraint("ck_study_events_correct", "correct IS NULL OR (correct >= 0 AND correct <= quantity)");
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Kind).HasMaxLength(32).IsRequired();
        // KHÔNG CHECK constraint cho kind (D31) — feature sau thêm loại mới không phải sửa DB.
        builder.Property(e => e.OccurredAt).IsRequired();
        builder.Property(e => e.LocalDate).IsRequired();
        // KHÔNG HasDefaultValue(1): EF coi giá trị CLR mặc định (0) của property non-null là "chưa
        // gán" và bỏ nó khỏi câu INSERT khi cột có default ở DB ⇒ ai đó (F7–F11, bảng dùng chung)
        // cố ý ghi quantity=0 sẽ bị Postgres âm thầm điền lại 1 (review F5 17/09/2026). Domain luôn
        // truyền quantity tường minh (StudyEvent.Create), không cần default ở tầng DB.
        builder.Property(e => e.Quantity).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.LocalDate });
        builder.HasIndex(e => new { e.UserId, e.Kind, e.OccurredAt }).IsDescending(false, false, true);
    }
}
