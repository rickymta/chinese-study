namespace AntFarm.Cms.Domain.Access;

/// <summary>
/// Bảng nối người dùng ↔ vai trò (schema `access.user_roles`, §5.1.1 W1, khoá kép UserId+RoleId).
/// Khác <c>access.user_roles</c> của chinese-backend: có thêm <see cref="AssignedBy"/> (ai gán —
/// null khi tự động lúc provision/bootstrap, có giá trị khi admin gán tay qua
/// <c>PUT /api/admin/users/{id}/roles</c>).
/// </summary>
public sealed class UserRole
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public Guid? AssignedBy { get; private set; }

    private UserRole()
    {
    }

    public static UserRole Create(Guid userId, Guid roleId, DateTime assignedAt, Guid? assignedBy = null) => new()
    {
        UserId = userId,
        RoleId = roleId,
        AssignedAt = assignedAt,
        AssignedBy = assignedBy
    };
}
