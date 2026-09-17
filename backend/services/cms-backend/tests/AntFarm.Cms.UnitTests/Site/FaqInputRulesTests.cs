using AntFarm.Cms.Application.Site.Validators;
using FluentAssertions;
using Xunit;

namespace AntFarm.Cms.UnitTests.Site;

/// <summary>Chuẩn hoá/kiểm định dạng <c>groupKey</c> FAQ (§5.2.3 W3b).</summary>
public class FaqInputRulesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_RongHoacKhoangTrang_TraVeGeneral(string? input) =>
        FaqInputRules.Normalize(input).Should().Be("general");

    [Fact]
    public void Normalize_CoGiaTri_Trim_LowerCase()
    {
        FaqInputRules.Normalize("  Hoc-Phi  ").Should().Be("hoc-phi");
    }

    [Theory]
    [InlineData("general", true)]
    [InlineData("hoc-phi", true)]
    [InlineData("a", true)]
    [InlineData("Hoc-Phi", false)] // chữ hoa — sai theo pattern (chuẩn hoá TRƯỚC khi kiểm)
    [InlineData("hoc phi", false)] // khoảng trắng
    [InlineData("", false)]
    public void GroupKeyPattern_KhopDungLuat(string value, bool expected) =>
        FaqInputRules.GroupKeyPattern().IsMatch(value).Should().Be(expected);

    [Fact]
    public void GroupKeyPattern_QuaBaMuoiHaiKyTu_BiTuChoi()
    {
        var tooLong = new string('a', 33);
        FaqInputRules.GroupKeyPattern().IsMatch(tooLong).Should().BeFalse();
    }
}
