using AntFarm.Chinese.Domain.Text;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Text;

public class VietnameseTextTests
{
    [Theory]
    [InlineData("Đường đi", "Duong di")]
    [InlineData("đ Đ", "d D")]
    [InlineData("ái", "ai")]
    [InlineData("yêu", "yeu")]
    [InlineData("yếu", "yeu")]
    public void RemoveDiacritics_TraVeDungBanKhongDau(string input, string expected) =>
        VietnameseText.RemoveDiacritics(input).Should().Be(expected);

    [Fact]
    public void RemoveDiacritics_NfdDauVao_ChoKetQuaNhuNfc()
    {
        // "ế" viết bằng NFD (e + dấu mũ + dấu sắc tách rời) phải cho cùng kết quả với NFC.
        var nfd = "ế".Normalize(System.Text.NormalizationForm.FormD);
        VietnameseText.RemoveDiacritics(nfd).Should().Be("e");
    }

    [Fact]
    public void YeuVaYeu_KhacNhauKhiConDau_GiongNhauKhiBoDau()
    {
        "yêu".Should().NotBe("yếu");
        VietnameseText.RemoveDiacritics("yêu").Should().Be(VietnameseText.RemoveDiacritics("yếu"));
        VietnameseText.RemoveDiacritics("yêu").Should().Be("yeu");
    }

    [Fact]
    public void NormalizeForSearch_BoNoiDungTrongNgoacTronVaKyTuKhongPhaiChuSo() =>
        VietnameseText.NormalizeForSearch("(trợ từ) Của; ~hậu tố").Should().Be("của hậu tố");

    [Fact]
    public void NormalizeForSearch_NfdDauVao_ChoKetQuaNhuNfc()
    {
        var nfd = "Yêu".Normalize(System.Text.NormalizationForm.FormD);
        VietnameseText.NormalizeForSearch(nfd).Should().Be("yêu");
    }

    [Fact]
    public void NormalizeForSearch_GomKhoangTrangThuaVaTrim() =>
        VietnameseText.NormalizeForSearch("  yêu   thích  ").Should().Be("yêu thích");

    [Theory]
    [InlineData("yêu", true)]
    [InlineData("yeu", false)]
    [InlineData("ai", false)]
    public void HasDiacritics_DungTheoDauSauChuanHoa(string input, bool expected) =>
        VietnameseText.HasDiacritics(input).Should().Be(expected);
}
