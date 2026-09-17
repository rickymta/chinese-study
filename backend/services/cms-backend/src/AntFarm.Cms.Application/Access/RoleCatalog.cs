using AntFarm.Cms.Domain.Access;

namespace AntFarm.Cms.Application.Access;

/// <summary>
/// Nguồn sự thật DUY NHẤT của bảng vai trò → quyền + tên tiếng Việt (R-W9, §3.2 hợp đồng
/// W1–W15) — <c>AccessSeeder</c> (Infrastructure) đọc danh mục này để chèn bù
/// <c>access.roles</c>/<c>access.permissions</c>/<c>access.role_permissions</c> lúc khởi động.
/// Quyền hiệu lực thật sự vẫn luôn đọc từ DB (<see cref="PermissionResolver"/>,
/// <see cref="UserAdminService.GetRolesCatalogAsync"/>) — danh mục ở đây chỉ là "giá trị khởi
/// tạo", admin không sửa được qua UI ở W1 (không có màn quản trị vai trò tuỳ biến).
/// </summary>
public static class RoleCatalog
{
    public sealed record RoleDefinition(string Code, string Name, IReadOnlyList<string> Permissions);

    public sealed record PermissionDefinition(string Code, string Description);

    public static readonly IReadOnlyList<PermissionDefinition> Permissions =
    [
        new(PermissionCodes.SiteManage, "Cấu hình site/SEO, ngôn ngữ, FAQ, trang tĩnh, banner/hero"),
        new(PermissionCodes.PostsManage, "Bài viết + danh mục (gồm xuất bản/gỡ)"),
        new(PermissionCodes.MediaManage, "Tải lên / sửa alt / xoá ảnh"),
        new(PermissionCodes.InboxManage, "Xem, đánh dấu, xuất CSV liên hệ + nhận tin"),
        new(PermissionCodes.AccountsManage, "Quản trị tài khoản nền tảng (identity) + công tắc đăng ký + thống kê"),
        new(PermissionCodes.UsersManage, "Xem người dùng CMS, gán vai trò CMS")
    ];

    // R-W9 (§3.2): bảng vai trò → quyền đã chốt trong hợp đồng.
    public static readonly IReadOnlyList<RoleDefinition> Roles =
    [
        new(RoleCodes.Admin, "Quản trị viên", PermissionCodes.All),
        new(RoleCodes.Editor, "Biên tập website", [PermissionCodes.SiteManage, PermissionCodes.PostsManage, PermissionCodes.MediaManage]),
        new(RoleCodes.Support, "Hỗ trợ người học", [PermissionCodes.InboxManage, PermissionCodes.AccountsManage])
    ];
}
