using AntFarm.Auth.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace AntFarm.Shared.UnitTests.Auth;

/// <summary>
/// Bài học MedDental: <c>public new string Policy</c> ở lớp con che thuộc tính kiểu tĩnh nhưng
/// ASP.NET Core đọc <see cref="IAuthorizeData.Policy"/> qua INTERFACE nên vẫn nhận null — mọi
/// [RequirePermission] thoái hoá thành [Authorize] trơn, không log, không lỗi biên dịch. Test
/// này khoá đúng hành vi Policy được gán qua CONSTRUCTOR của lớp CƠ SỞ.
/// </summary>
public class RequirePermissionAttributeTests
{
    [Fact]
    public void Constructor_GanPolicyDungTienTo()
    {
        var attribute = new RequirePermissionAttribute("study.use");

        attribute.Policy.Should().Be("Permission:study.use");
    }

    [Fact]
    public void Policy_DocQuaInterfaceIAuthorizeData_KhongBiNull()
    {
        // Đây chính là cách ASP.NET Core AuthorizationMiddleware đọc Policy thật sự — qua
        // interface, không qua kiểu tĩnh. Nếu có ai lỡ khai "public new string Policy" ở lớp
        // con, test này vẫn PASS (vì chỉ test lớp gốc) NHƯNG failing pattern y hệt sẽ lộ ra nếu
        // thêm lớp con tương tự — giữ test làm tài liệu sống cho bẫy này.
        IAuthorizeData data = new RequirePermissionAttribute("content.manage");

        data.Policy.Should().Be("Permission:content.manage");
    }

    [Fact]
    public void KhongCoThanhVienNaoTenPolicyKhaiBangNew()
    {
        // Quét reflection: đảm bảo RequirePermissionAttribute không có thành viên "Policy" khai
        // riêng (chỉ kế thừa từ AuthorizeAttribute) — nếu có, DeclaringType sẽ là chính lớp này
        // thay vì AuthorizeAttribute.
        var property = typeof(RequirePermissionAttribute).GetProperty("Policy");
        property.Should().NotBeNull();
        property!.DeclaringType.Should().Be(typeof(AuthorizeAttribute));
    }
}
