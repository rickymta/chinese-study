namespace AntFarm.Cms.Domain.Access;

/// <summary>Quyền cục bộ (schema `access.permissions`, §5.1.1 W1) — khoá chính là mã quyền (vd "site.manage"), danh mục hệ thống chèn bù bởi <c>AccessSeeder</c>.</summary>
public sealed class Permission
{
    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    private Permission()
    {
    }

    public static Permission Create(string code, string description) => new()
    {
        Code = code,
        Description = description
    };
}
