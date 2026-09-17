using AntFarm.Chinese.Domain.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations;

/// <summary>Bảng access.role_permissions (§5.1.2, migration F3_Access) — khoá kép (role_id, permission_code), cả hai FK CASCADE.</summary>
public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions", "access");

        builder.HasKey(rp => new { rp.RoleId, rp.PermissionCode });

        // Không dùng navigation property (Domain giữ entity nối bảng tối giản) — cấu hình quan hệ
        // qua HasOne<TRelated>() tổng quát vẫn tạo được FK đúng.
        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(rp => rp.PermissionCode)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
