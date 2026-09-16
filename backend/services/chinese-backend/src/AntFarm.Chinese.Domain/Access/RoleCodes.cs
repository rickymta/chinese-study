namespace AntFarm.Chinese.Domain.Access;

/// <summary>Danh mục vai trò cục bộ của chinese-backend (R-P2, §3.3) — chèn bù idempotent bởi
/// <c>AccessSeeder</c>; ánh xạ vai trò → quyền cũng do seeder ghi vào DB (không hardcode logic
/// phân quyền ở đây, Domain chỉ giữ MÃ vai trò để nơi khác không gõ tay chuỗi).</summary>
public static class RoleCodes
{
    public const string Admin = "admin";
    public const string Learner = "learner";

    public static readonly IReadOnlyList<string> All = [Admin, Learner];
}
