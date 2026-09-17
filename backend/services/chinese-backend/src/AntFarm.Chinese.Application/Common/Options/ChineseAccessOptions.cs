namespace AntFarm.Chinese.Application.Common.Options;

/// <summary>
/// R-P5: vai trò mặc định cấp cho người dùng MỚI provision. Bind từ section "ChineseAccess" —
/// POCO singleton INSTANCE (không IOptions&lt;T&gt;, giống Jwt/AuthOptions của identity-service,
/// §5.2.0.5) để Application (không FrameworkReference AspNetCore.App) tiêm thẳng được.
/// </summary>
public sealed class ChineseAccessOptions
{
    /// <summary>Để rỗng ⇒ người dùng mới có 0 quyền (fail-closed, không suy quyền ngầm — D17).</summary>
    public string[] DefaultRoles { get; init; } = ["learner"];
}
