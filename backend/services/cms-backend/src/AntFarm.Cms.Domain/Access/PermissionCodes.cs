namespace AntFarm.Cms.Domain.Access;

/// <summary>
/// Danh mục quyền cục bộ của cms-backend (R-W9, §3.2 hợp đồng W1–W15) — nguồn sự thật DUY NHẤT
/// là DB `access.permissions` (do <c>AccessSeeder</c> chèn bù theo <c>RoleCatalog</c>); hằng số
/// ở đây chỉ để code C# không gõ tay chuỗi rải rác.
/// </summary>
public static class PermissionCodes
{
    /// <summary>Cấu hình site/SEO, ngôn ngữ, FAQ, trang tĩnh, banner/hero.</summary>
    public const string SiteManage = "site.manage";

    /// <summary>Bài viết + danh mục (gồm xuất bản/gỡ).</summary>
    public const string PostsManage = "posts.manage";

    /// <summary>Tải lên / sửa alt / xoá ảnh.</summary>
    public const string MediaManage = "media.manage";

    /// <summary>Xem, đánh dấu, xuất CSV liên hệ + nhận tin.</summary>
    public const string InboxManage = "inbox.manage";

    /// <summary>Quản trị tài khoản nền tảng (identity) + công tắc đăng ký + thống kê.</summary>
    public const string AccountsManage = "accounts.manage";

    /// <summary>Xem người dùng CMS, gán vai trò CMS.</summary>
    public const string UsersManage = "users.manage";

    public static readonly IReadOnlyList<string> All =
        [SiteManage, PostsManage, MediaManage, InboxManage, AccountsManage, UsersManage];
}
