using AntFarm.Cms.Domain.Site;
using FluentAssertions;
using Xunit;

namespace AntFarm.Cms.UnitTests.Site;

/// <summary>Whitelist khoá cấu hình site/SEO (§5.2.3 W3a, test bắt buộc #1) — nguồn duy nhất cho validator + seeder.</summary>
public class SiteSettingKeysTests
{
    [Fact]
    public void CoDungMuoiMotKhoa()
    {
        SiteSettingKeys.All.Should().HaveCount(11);
        SiteSettingKeys.ByKey.Should().HaveCount(11);
    }

    [Fact]
    public void MoiKhoa_GiaTriMacDinh_HopLeTheoChinhLuatCuaNo()
    {
        foreach (var definition in SiteSettingKeys.All)
            definition.IsValid(definition.DefaultValue).Should().BeTrue(
                $"giá trị mặc định của '{definition.Key}' phải hợp lệ theo chính luật của nó");
    }

    [Theory]
    [InlineData(SiteSettingKeys.SocialFacebook)]
    [InlineData(SiteSettingKeys.SocialYoutube)]
    [InlineData(SiteSettingKeys.SocialTiktok)]
    public void SocialUrl_HttpKhongPhaiHttps_BiTuChoi(string key)
    {
        SiteSettingKeys.ByKey[key].IsValid("http://facebook.com/antfarm").Should().BeFalse();
    }

    [Fact]
    public void HomeHeroMode_GiaTriLa_BiTuChoi()
    {
        SiteSettingKeys.ByKey[SiteSettingKeys.HomeHeroMode].IsValid("abc").Should().BeFalse();
    }

    [Fact]
    public void HomeHeroMode_BannersHoacStatic_HopLe()
    {
        SiteSettingKeys.ByKey[SiteSettingKeys.HomeHeroMode].IsValid("banners").Should().BeTrue();
        SiteSettingKeys.ByKey[SiteSettingKeys.HomeHeroMode].IsValid("static").Should().BeTrue();
    }

    [Fact]
    public void SiteName_Rong_BiTuChoi()
    {
        SiteSettingKeys.ByKey[SiteSettingKeys.SiteName].IsValid("").Should().BeFalse();
    }

    [Fact]
    public void ContactEmail_Rong_HopLe()
    {
        SiteSettingKeys.ByKey[SiteSettingKeys.ContactEmail].IsValid("").Should().BeTrue();
    }

    [Fact]
    public void ContactEmail_SaiDinhDang_BiTuChoi()
    {
        SiteSettingKeys.ByKey[SiteSettingKeys.ContactEmail].IsValid("khong-phai-email").Should().BeFalse();
    }
}
