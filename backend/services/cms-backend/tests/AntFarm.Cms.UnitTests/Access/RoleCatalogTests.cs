using AntFarm.Cms.Application.Access;
using AntFarm.Cms.Domain.Access;
using FluentAssertions;
using Xunit;

namespace AntFarm.Cms.UnitTests.Access;

/// <summary>R-W9 (§3.2 hợp đồng W1–W15) — bảng vai trò → quyền là nguồn duy nhất cho <c>AccessSeeder</c>.</summary>
public class RoleCatalogTests
{
    [Fact]
    public void Admin_CoDuSauQuyen()
    {
        var admin = RoleCatalog.Roles.Single(r => r.Code == RoleCodes.Admin);

        admin.Permissions.Should().BeEquivalentTo(PermissionCodes.All);
        admin.Permissions.Should().HaveCount(6);
    }

    [Fact]
    public void Editor_CoDungBaQuyen()
    {
        var editor = RoleCatalog.Roles.Single(r => r.Code == RoleCodes.Editor);

        editor.Permissions.Should().BeEquivalentTo([
            PermissionCodes.SiteManage, PermissionCodes.PostsManage, PermissionCodes.MediaManage
        ]);
    }

    [Fact]
    public void Support_CoDungHaiQuyen()
    {
        var support = RoleCatalog.Roles.Single(r => r.Code == RoleCodes.Support);

        support.Permissions.Should().BeEquivalentTo([
            PermissionCodes.InboxManage, PermissionCodes.AccountsManage
        ]);
    }

    [Fact]
    public void MoiMaQuyenTrongCatalog_ThuocPermissionCodesAll()
    {
        var allCodesInRoles = RoleCatalog.Roles.SelectMany(r => r.Permissions).Distinct();
        var allCodesInPermissions = RoleCatalog.Permissions.Select(p => p.Code);

        allCodesInRoles.Should().BeSubsetOf(PermissionCodes.All);
        allCodesInPermissions.Should().BeEquivalentTo(PermissionCodes.All);
    }

    [Fact]
    public void CoDungBaVaiTro_KhopVoiRoleCodesAll()
    {
        RoleCatalog.Roles.Select(r => r.Code).Should().BeEquivalentTo(RoleCodes.All, options => options.WithStrictOrdering());
    }
}
