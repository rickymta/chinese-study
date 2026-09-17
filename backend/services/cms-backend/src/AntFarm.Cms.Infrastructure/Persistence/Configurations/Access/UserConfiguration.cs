using AntFarm.Cms.Domain.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Cms.Infrastructure.Persistence.Configurations.Access;

/// <summary>Bảng access.users (§5.1.1, migration W1_Access).</summary>
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
        // thường (schema §5.1.1 "INDEX ix_users_email_lower ON (lower(email))"), KHÔNG phải khoá
        // duy nhất — email đã unique bên identity-service, ở đây không ép buộc.
        builder.Property<string>("EmailLower")
            .HasComputedColumnSql("lower(email)", stored: true);
        builder.HasIndex("EmailLower").HasDatabaseName("ix_users_email_lower");
    }
}
