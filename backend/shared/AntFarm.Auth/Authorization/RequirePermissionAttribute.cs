using Microsoft.AspNetCore.Authorization;

namespace AntFarm.Auth.Authorization;

/// <summary>
/// Yêu cầu người dùng có quyền cụ thể trên DB của service (R-P1). ⚠️ KHÔNG khai
/// <c>public new string Policy</c> ở lớp con hay bất kỳ đâu — <c>new</c> chỉ CHE thuộc tính
/// kiểu tĩnh, ASP.NET Core đọc <see cref="IAuthorizeData.Policy"/> qua interface nên vẫn nhận
/// giá trị null của <see cref="AuthorizeAttribute"/>, mọi [RequirePermission] thoái hoá thành
/// [Authorize] trơn — không log, không lỗi biên dịch (bài học MedDental, ghi trong CLAUDE.md).
/// </summary>
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission) => Policy = $"Permission:{permission}";
}
