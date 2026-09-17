using AntFarm.Chinese.Application.Common.Options;
using AntFarm.Chinese.Domain.Access;

namespace AntFarm.Chinese.Application.Access;

/// <summary>
/// Quyết định THUẦN (không I/O) tập vai trò gán cho người dùng MỚI provision (R-P5 + nửa "tạo
/// mới" của R-P6) — tách khỏi <see cref="UserProvisioningService"/> để unit test không cần DB thật.
/// </summary>
public static class DefaultRoleAssignmentPolicy
{
    /// <summary>Vai trò mặc định (<see cref="ChineseAccessOptions.DefaultRoles"/>) hợp với "admin" nếu email nằm trong danh sách bootstrap (R-P6). Rỗng ⇒ 0 quyền (fail-closed, D17).</summary>
    public static IReadOnlySet<string> Resolve(string email, ChineseAccessOptions accessOptions, ChineseAdminOptions adminOptions)
    {
        var roles = accessOptions.DefaultRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (IsBootstrapAdmin(email, adminOptions))
            roles.Add(RoleCodes.Admin);

        return roles;
    }

    public static bool IsBootstrapAdmin(string email, ChineseAdminOptions adminOptions) =>
        adminOptions.BootstrapEmails.Any(e => string.Equals(e.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));
}
