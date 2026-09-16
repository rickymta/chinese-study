using AntFarm.Chinese.Application.Access;
using AntFarm.Chinese.Application.Common.Options;
using AntFarm.Chinese.Domain.Access;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Access;

/// <summary>R-P5/R-P6 (nửa "tạo mới") — quyết định thuần, không DB.</summary>
public class DefaultRoleAssignmentPolicyTests
{
    [Fact]
    public void Resolve_EmailThuong_ChiNhanVaiTroMacDinh()
    {
        var access = new ChineseAccessOptions { DefaultRoles = ["learner"] };
        var admin = new ChineseAdminOptions { BootstrapEmails = [] };

        var roles = DefaultRoleAssignmentPolicy.Resolve("hocvien@vidu.com", access, admin);

        roles.Should().BeEquivalentTo(["learner"]);
    }

    [Fact]
    public void Resolve_DefaultRolesRong_TraVeRong_KhongTuSuyQuyen()
    {
        var access = new ChineseAccessOptions { DefaultRoles = [] };
        var admin = new ChineseAdminOptions { BootstrapEmails = [] };

        var roles = DefaultRoleAssignmentPolicy.Resolve("hocvien@vidu.com", access, admin);

        roles.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_EmailBootstrap_ThemVaiTroAdmin()
    {
        var access = new ChineseAccessOptions { DefaultRoles = ["learner"] };
        var admin = new ChineseAdminOptions { BootstrapEmails = ["admin@vidu.com"] };

        var roles = DefaultRoleAssignmentPolicy.Resolve("admin@vidu.com", access, admin);

        roles.Should().BeEquivalentTo([RoleCodes.Learner, RoleCodes.Admin]);
    }

    [Fact]
    public void Resolve_EmailBootstrap_KhongPhanBietHoaThuongVaKhoangTrangThua()
    {
        var access = new ChineseAccessOptions { DefaultRoles = [] };
        var admin = new ChineseAdminOptions { BootstrapEmails = [" Admin@Vidu.com "] };

        var roles = DefaultRoleAssignmentPolicy.Resolve("admin@vidu.com", access, admin);

        roles.Should().BeEquivalentTo([RoleCodes.Admin]);
    }

    [Fact]
    public void IsBootstrapAdmin_EmailKhongNamTrongDanhSach_TraVeFalse()
    {
        var admin = new ChineseAdminOptions { BootstrapEmails = ["admin@vidu.com"] };

        DefaultRoleAssignmentPolicy.IsBootstrapAdmin("khac@vidu.com", admin).Should().BeFalse();
    }
}
