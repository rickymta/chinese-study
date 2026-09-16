namespace AntFarm.Chinese.Domain.Access;

/// <summary>Bảng nối người dùng ↔ vai trò (schema `access.user_roles`, §5.1.2, khoá kép UserId+RoleId).</summary>
public sealed class UserRole
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    private UserRole()
    {
    }

    public static UserRole Create(Guid userId, Guid roleId, DateTime assignedAt) => new()
    {
        UserId = userId,
        RoleId = roleId,
        AssignedAt = assignedAt
    };
}
