using AntFarm.Chinese.Domain.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations;

/// <summary>Bảng access.users (§5.1.2, migration F3_Access).</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "access");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(254).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.TimeZone).HasMaxLength(64).IsRequired();
        builder.Property(u => u.FirstSeenAt).IsRequired();
        builder.Property(u => u.LastSeenAt).IsRequired();

        // Cột sinh (generated, stored) lower(email) — CHỈ để lập chỉ mục tìm không phân biệt hoa
        // thường (schema §5.1.2 "INDEX (lower(email))", dùng bởi tìm kiếm người dùng F4), KHÔNG
        // phải khoá duy nhất — email đã unique bên identity-service (R-N1), ở đây không ép buộc.
        builder.Property<string>("EmailLower")
            .HasComputedColumnSql("lower(email)", stored: true);
        builder.HasIndex("EmailLower").HasDatabaseName("ix_users_email_lower");
    }
}
