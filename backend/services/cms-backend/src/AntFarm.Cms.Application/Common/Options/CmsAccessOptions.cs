namespace AntFarm.Cms.Application.Common.Options;

/// <summary>
/// R-W2 (fail-closed): vai trò mặc định cấp cho người dùng MỚI provision. Bind từ section
/// "CmsAccess" — POCO singleton INSTANCE (không IOptions&lt;T&gt;, giống Jwt/AuthOptions của
/// identity-service) để Application (không FrameworkReference AspNetCore.App) tiêm thẳng được.
/// </summary>
public sealed class CmsAccessOptions
{
    /// <summary>
    /// Mặc định RỖNG (khác chinese-backend — DefaultRoles=["learner"]): người mới vào admin
    /// KHÔNG nhận vai trò nào cho tới khi admin gán tay (R-W2, fail-closed).
    /// </summary>
    public string[] DefaultRoles { get; init; } = [];
}
