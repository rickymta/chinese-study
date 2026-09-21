namespace AntFarm.Cms.Domain.Access;

/// <summary>
/// Danh mục vai trò cục bộ của cms-backend (R-W9, §3.2 hợp đồng W1–W15) — chèn bù idempotent bởi
/// <c>AccessSeeder</c>; ánh xạ vai trò → quyền + tên tiếng Việt nằm ở
/// <c>AntFarm.Cms.Application.Access.RoleCatalog</c> (nguồn duy nhất cho seeder). Domain chỉ giữ
/// MÃ vai trò để nơi khác không gõ tay chuỗi.
/// </summary>
public static class RoleCodes
{
    public const string Admin = "admin";
    public const string Editor = "editor";
    public const string Support = "support";

    /// <summary>Thứ tự cố định dùng để sắp <c>GET /api/admin/roles</c> (§6.1).</summary>
    public static readonly IReadOnlyList<string> All = [Admin, Editor, Support];
}
