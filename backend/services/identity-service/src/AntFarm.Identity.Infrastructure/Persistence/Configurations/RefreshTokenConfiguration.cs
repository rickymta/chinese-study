using AntFarm.Identity.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Identity.Infrastructure.Persistence.Configurations;

/// <summary>Bảng identity.refresh_tokens (§5.1.1, migration F2_Accounts; M1_MobileClient thêm cột kênh mobile).</summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // M1 §5.1.1: check constraint đặt tên tường minh để khớp CHÍNH XÁC lệnh SQL trong hợp
        // đồng (dòng cũ trước M1 migrate thành 'web' qua HasDefaultValue bên dưới).
        builder.ToTable("refresh_tokens", "identity", t => t.HasCheckConstraint("ck_refresh_tokens_client_type", "client_type IN ('web', 'mobile')"));

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.HasIndex(t => t.AccountId);
        builder.HasIndex(t => t.FamilyId);

        builder.Property(t => t.RevokeReason).HasMaxLength(32);
        builder.Property(t => t.UserAgent).HasMaxLength(300);
        builder.Property(t => t.CreatedIp).HasMaxLength(45);

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();

        // M1 (RM-A3): lưu chuỗi 'web'/'mobile' thường (KHÔNG dùng EnumToStringConverter mặc định
        // — sẽ ra "Web"/"Mobile" viết hoa, lệch đúng giá trị SQL trong hợp đồng §5.1.1).
        builder.Property(t => t.ClientType)
            .HasConversion(
                v => v == RefreshClientType.Mobile ? "mobile" : "web",
                v => v == "mobile" ? RefreshClientType.Mobile : RefreshClientType.Web)
            .HasMaxLength(16)
            .HasDefaultValue(RefreshClientType.Web)
            .IsRequired();

        builder.Property(t => t.ClientApp).HasMaxLength(64);
        builder.Property(t => t.DeviceName).HasMaxLength(100);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
