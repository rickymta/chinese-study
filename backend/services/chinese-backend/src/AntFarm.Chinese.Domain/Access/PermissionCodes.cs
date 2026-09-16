namespace AntFarm.Chinese.Domain.Access;

/// <summary>Danh mục quyền cục bộ của chinese-backend (R-P2, §3.3) — nguồn sự thật DUY NHẤT là
/// DB `access.permissions` (do <c>AccessSeeder</c> chèn bù từ danh sách này); hằng số ở đây chỉ để
/// code C# không gõ tay chuỗi rải rác (tránh gõ sai "study.use" thành "study_use"...).</summary>
public static class PermissionCodes
{
    /// <summary>Dùng mọi chức năng học của service.</summary>
    public const string StudyUse = "study.use";

    /// <summary>Soạn/sửa/xuất bản bài học, quiz, duyệt nghĩa từ vựng.</summary>
    public const string ContentManage = "content.manage";

    /// <summary>Xem người dùng của service, gán vai trò.</summary>
    public const string UsersManage = "users.manage";

    public static readonly IReadOnlyList<string> All = [StudyUse, ContentManage, UsersManage];
}
