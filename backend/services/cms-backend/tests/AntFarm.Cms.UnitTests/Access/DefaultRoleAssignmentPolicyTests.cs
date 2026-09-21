using AntFarm.Cms.Application.Access;
using AntFarm.Cms.Application.Common.Options;
using AntFarm.Cms.Domain.Access;
using FluentAssertions;
using Xunit;

namespace AntFarm.Cms.UnitTests.Access;

/// <summary>R-W2 (fail-closed)/R-W10 (nửa "tạo mới") — quyết định thuần, không DB.</summary>
public class DefaultRoleAssignmentPolicyTests
{
    [Fact]
    public void Resolve_EmailThuong_DefaultRolesRong_TraVeRong_KhongTuSuyQuyen()
    {
        var access = new CmsAccessOptions { DefaultRoles = [] };
        var admin = new CmsAdminOptions { BootstrapEmails = [] };

        var roles = DefaultRoleAssignmentPolicy.Resolve("bien-tap@vidu.com", access, admin);

        roles.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_CoCauHinhDefaultRoles_TraVeDungCauHinh()
    {
        var access = new CmsAccessOptions { DefaultRoles = ["editor"] };
        var admin = new CmsAdminOptions { BootstrapEmails = [] };

        var roles = DefaultRoleAssignmentPolicy.Resolve("bien-tap@vidu.com", access, admin);

        roles.Should().BeEquivalentTo([RoleCodes.Editor]);
    }

    [Fact]
    public void Resolve_EmailBootstrap_ThemVaiTroAdmin()
    {
        var access = new CmsAccessOptions { DefaultRoles = [] };
        var admin = new CmsAdminOptions { BootstrapEmails = ["admin@vidu.com"] };

        var roles = DefaultRoleAssignmentPolicy.Resolve("admin@vidu.com", access, admin);

        roles.Should().BeEquivalentTo([RoleCodes.Admin]);
    }

    [Fact]
    public void Resolve_EmailBootstrap_KhongPhanBietHoaThuongVaKhoangTrangThua()
    {
        var access = new CmsAccessOptions { DefaultRoles = [] };
        var admin = new CmsAdminOptions { BootstrapEmails = [" Admin@Vidu.com "] };

        var roles = DefaultRoleAssignmentPolicy.Resolve("admin@vidu.com", access, admin);

        roles.Should().BeEquivalentTo([RoleCodes.Admin]);
    }

    [Fact]
    public void IsBootstrapAdmin_EmailKhongNamTrongDanhSach_TraVeFalse()
    {
        var admin = new CmsAdminOptions { BootstrapEmails = ["admin@vidu.com"] };

        DefaultRoleAssignmentPolicy.IsBootstrapAdmin("khac@vidu.com", admin).Should().BeFalse();
    }
}
