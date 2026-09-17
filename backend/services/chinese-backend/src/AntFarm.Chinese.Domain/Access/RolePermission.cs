namespace AntFarm.Chinese.Domain.Access;

/// <summary>Bảng nối vai trò ↔ quyền (schema `access.role_permissions`, §5.1.2, khoá kép RoleId+PermissionCode).</summary>
public sealed class RolePermission
{
    public Guid RoleId { get; private set; }
    public string PermissionCode { get; private set; } = null!;

    private RolePermission()
    {
    }

    public static RolePermission Create(Guid roleId, string permissionCode) => new()
    {
        RoleId = roleId,
        PermissionCode = permissionCode
    };
}
