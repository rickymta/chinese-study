namespace AntFarm.Auth.Authorization;

/// <summary>
/// Nguồn sự thật DUY NHẤT của quyền hiệu lực (R-P1) — mỗi service ngôn ngữ tự hiện thực trên
/// DB riêng (users → user_roles → role_permissions), KHÔNG suy quyền từ claim JWT.
/// </summary>
public interface IPermissionResolver
{
    Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken);
}
