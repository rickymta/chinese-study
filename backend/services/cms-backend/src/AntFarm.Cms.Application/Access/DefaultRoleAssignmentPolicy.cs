using AntFarm.Cms.Application.Common.Options;
using AntFarm.Cms.Domain.Access;

namespace AntFarm.Cms.Application.Access;

/// <summary>
/// Quyết định THUẦN (không I/O) tập vai trò gán cho người dùng MỚI provision (R-W2 + R-W10 nửa
/// "tạo mới") — tách khỏi <see cref="UserProvisioningService"/> để unit test không cần DB thật.
/// Chép khuôn <c>AntFarm.Chinese.Application.Access.DefaultRoleAssignmentPolicy</c>.
/// </summary>
public static class DefaultRoleAssignmentPolicy
{
    /// <summary>Vai trò mặc định (<see cref="CmsAccessOptions.DefaultRoles"/> — rỗng mặc định, R-W2) hợp với "admin" nếu email nằm trong danh sách bootstrap (R-W10). Rỗng ⇒ 0 quyền (fail-closed).</summary>
    public static IReadOnlySet<string> Resolve(string email, CmsAccessOptions accessOptions, CmsAdminOptions adminOptions)
    {
        var roles = accessOptions.DefaultRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (IsBootstrapAdmin(email, adminOptions))
            roles.Add(RoleCodes.Admin);

        return roles;
    }

    public static bool IsBootstrapAdmin(string email, CmsAdminOptions adminOptions) =>
        adminOptions.BootstrapEmails.Any(e => string.Equals(e.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));
}
