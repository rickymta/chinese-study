namespace AntFarm.Chinese.Domain.Access;

/// <summary>Danh mục vai trò cục bộ của chinese-backend (R-P2, §3.3) — chèn bù idempotent bởi
/// <c>AccessSeeder</c>; ánh xạ vai trò → quyền cũng do seeder ghi vào DB (không hardcode logic
/// phân quyền ở đây, Domain chỉ giữ MÃ vai trò để nơi khác không gõ tay chuỗi).</summary>
public static class RoleCodes
{
    public const string Admin = "admin";
    public const string Learner = "learner";

    public static readonly IReadOnlyList<string> All = [Admin, Learner];

    /// <summary>
    /// Mô tả tiếng Việt cho <c>GET /api/admin/roles</c> (D39, §6.3) — F3 chưa thêm cột
    /// <c>description</c> vào <c>access.roles</c> nên F4 lấy từ hằng số ở đây thay vì migration
    /// mới (§5.1.2: "F4 không đổi schema").
    /// </summary>
    public static string Describe(string code) => code switch
    {
        Admin => "Toàn quyền: học, soạn nội dung, quản lý người dùng",
        Learner => "Dùng các chức năng học",
        _ => string.Empty
    };
}
