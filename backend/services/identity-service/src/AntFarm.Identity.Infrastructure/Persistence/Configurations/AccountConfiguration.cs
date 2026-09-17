using AntFarm.Identity.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Identity.Infrastructure.Persistence.Configurations;

/// <summary>Bảng identity.accounts (§5.1.1, migration F2_Accounts).</summary>
public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts", "identity");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Email).HasMaxLength(254).IsRequired();
        builder.Property(a => a.EmailNormalized).HasMaxLength(254).IsRequired();
        builder.HasIndex(a => a.EmailNormalized).IsUnique();

        builder.Property(a => a.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PasswordHash).IsRequired();
        builder.Property(a => a.TimeZone).HasMaxLength(64).IsRequired().HasDefaultValue("Asia/Ho_Chi_Minh");

        builder.Property(a => a.IsActive).HasDefaultValue(true);
        builder.Property(a => a.FailedLoginCount).HasDefaultValue(0);

        builder.Property(a => a.PasswordChangedAt).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
    }
}
