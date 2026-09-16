using AntFarm.Chinese.Domain.Common;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Common;

/// <summary>R-T2/RK36 — chuẩn hoá múi giờ claim "zoneinfo".</summary>
public class TimeZoneCatalogTests
{
    [Fact]
    public void Normalize_IdHopLe_GiuNguyen()
    {
        TimeZoneCatalog.Normalize("Asia/Ho_Chi_Minh").Should().Be("Asia/Ho_Chi_Minh");
        TimeZoneCatalog.Normalize("Europe/Berlin").Should().Be("Europe/Berlin");
    }

    [Fact]
    public void Normalize_BiDanhAsiaSaigon_QuyVeAsiaHoChiMinh()
    {
        TimeZoneCatalog.Normalize("Asia/Saigon").Should().Be("Asia/Ho_Chi_Minh");
    }

    [Fact]
    public void Normalize_IdKhongTonTai_TraVeMacDinh()
    {
        TimeZoneCatalog.Normalize("Khong/Ton_Tai").Should().Be(TimeZoneCatalog.DefaultTimeZoneId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_RongHoacKhoangTrang_TraVeMacDinh(string? input)
    {
        TimeZoneCatalog.Normalize(input).Should().Be(TimeZoneCatalog.DefaultTimeZoneId);
    }

    [Fact]
    public void Normalize_KhongPhanBietHoaThuong_QuyBiDanh()
    {
        TimeZoneCatalog.Normalize("asia/saigon").Should().Be("Asia/Ho_Chi_Minh");
    }

    [Fact]
    public void IsValid_IdHopLe_TraVeTrue()
    {
        TimeZoneCatalog.IsValid("Asia/Ho_Chi_Minh").Should().BeTrue();
    }

    [Fact]
    public void IsValid_IdKhongHopLe_TraVeFalse()
    {
        TimeZoneCatalog.IsValid("Khong/Ton_Tai").Should().BeFalse();
        TimeZoneCatalog.IsValid(null).Should().BeFalse();
    }
}
