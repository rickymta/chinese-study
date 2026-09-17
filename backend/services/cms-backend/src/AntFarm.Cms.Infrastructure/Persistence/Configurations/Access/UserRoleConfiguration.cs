using AntFarm.Cms.Domain.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Cms.Infrastructure.Persistence.Configurations.Access;

/// <summary>
/// Bảng access.user_roles (§5.1.1, migration W1_Access) — khoá kép (user_id, role_id); user
/// CASCADE (xoá user thì gỡ luôn gán vai trò), role RESTRICT (không được xoá vai trò còn người
/// dùng đang giữ). <c>assigned_by</c> KHÔNG có khoá ngoại — chỉ ghi lại id người thao tác lúc gán
/// (có thể null khi tự động lúc provision/bootstrap), không ràng buộc vòng đời với người đó.
/// </summary>
public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles", "access");

        builder.HasKey(ur => new { ur.UserId, ur.RoleId });
        builder.Property(ur => ur.AssignedAt).IsRequired();
        builder.Property(ur => ur.AssignedBy).IsRequired(false);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
