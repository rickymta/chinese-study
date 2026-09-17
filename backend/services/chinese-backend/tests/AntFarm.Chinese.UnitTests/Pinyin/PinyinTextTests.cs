using AntFarm.Chinese.Domain.Pinyin;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Pinyin;

public class PinyinTextTests
{
    // ---- NormalizeNumbered ----

    [Theory]
    [InlineData("lü4", "lv4")]
    [InlineData("lu:4", "lv4")]
    [InlineData("LÜ4", "Lv4")]
    [InlineData("ni3  hao3 ", "ni3 hao3")]
    [InlineData("Bei3 jing1", "Bei3 jing1")]
    [InlineData("  ma3  ", "ma3")]
    public void NormalizeNumbered_DauVaoHopLe_TraVeDangChuan(string input, string expected) =>
        PinyinText.NormalizeNumbered(input).Should().Be(expected);

    [Theory]
    [InlineData("ma")]
    [InlineData("ma6")]
    [InlineData("ma0")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ma3 hao")]
    public void NormalizeNumbered_DauVaoSai_TraVeNull(string input) =>
        PinyinText.NormalizeNumbered(input).Should().BeNull();

    [Fact]
    public void NormalizeNumbered_Null_TraVeNull() =>
        PinyinText.NormalizeNumbered(null!).Should().BeNull();

    // ---- FromToneMarks ----

    [Theory]
    [InlineData("nǐ hǎo", "ni3 hao3")]
    [InlineData("lǜ", "lv4")]
    [InlineData("ma", "ma5")]
    [InlineData("Xī'ān", "Xi1 an1")]
    [InlineData("nǚ", "nv3")]
    [InlineData("mā", "ma1")]
    [InlineData("má", "ma2")]
    [InlineData("mǎ", "ma3")]
    [InlineData("mà", "ma4")]
    public void FromToneMarks_DauVaoHopLe_TraVeSoThanhDung(string marked, string expected) =>
        PinyinText.FromToneMarks(marked).Should().Be(expected);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromToneMarks_DauVaoRong_TraVeNull(string input) =>
        PinyinText.FromToneMarks(input).Should().BeNull();

    // ---- ToSearchKey ----

    [Theory]
    [InlineData("Ni3 hao3", "nihao")]
    [InlineData("ma1", "ma")]
    [InlineData("Xi1 an1", "xian")]
    public void ToSearchKey_BoSoVaKhoangTrang(string numbered, string expected) =>
        PinyinText.ToSearchKey(numbered).Should().Be(expected);

    // ---- TryParseSyllable ----

    [Theory]
    [InlineData("lv4", true, "lv", 4)]
    [InlineData("Ma1", true, "ma", 1)]
    [InlineData("zhuang4", true, "zhuang", 4)]
    [InlineData("ma", false, "", 0)]
    [InlineData("ma6", false, "", 0)]
    [InlineData("Ma", false, "", 0)]
    public void TryParseSyllable_KetQuaDung(string token, bool expectedSuccess, string expectedSyllable, int expectedTone)
    {
        var success = PinyinText.TryParseSyllable(token, out var syllable, out var tone);

        success.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            syllable.Should().Be(expectedSyllable);
            tone.Should().Be(expectedTone);
        }
    }

    // ---- ToMarked (§5.3.C) ----

    [Theory]
    [InlineData("lve4", "lüè")]
    [InlineData("gui4", "guì")]
    [InlineData("liu2", "liú")]
    [InlineData("er2", "ér")]
    [InlineData("r5", "r")]
    [InlineData("ni3", "nǐ")]
    [InlineData("hao3", "hǎo")]
    [InlineData("zhuang4", "zhuàng")]
    [InlineData("xue2", "xué")]
    [InlineData("jiong3", "jiǒng")]
    [InlineData("ou1", "ōu")]
    [InlineData("lv5", "lü")]
    [InlineData("nv3", "nǚ")]
    [InlineData("A1", "Ā")]
    [InlineData("huo3", "huǒ")]
    [InlineData("ma1", "mā")]
    [InlineData("ma2", "má")]
    [InlineData("ma3", "mǎ")]
    [InlineData("ma4", "mà")]
    public void ToMarked_MotAmTiet_DatDauDung(string numbered, string expectedMarked) =>
        PinyinText.ToMarked(numbered).Should().Be(expectedMarked);

    [Fact]
    public void ToMarked_NhieuAmTiet_TachBangKhoangTrang() =>
        PinyinText.ToMarked("Ou1 zhou1").Should().Be("Ōu zhōu");

    [Fact]
    public void ToMarked_NiHao_TachBangKhoangTrang() =>
        PinyinText.ToMarked("ni3 hao3").Should().Be("nǐ hǎo");

    [Theory]
    [InlineData("xx9")]
    [InlineData("123")]
    [InlineData("")]
    public void ToMarked_TokenSai_GiuNguyen(string input) =>
        PinyinText.ToMarked(input).Should().Be(input);

    // ---- PinyinSyllable.IsValidKey (R5-1) ----

    [Theory]
    [InlineData("ju", true)]
    [InlineData("nv", true)]
    [InlineData("lv", true)]
    [InlineData("nve", true)]
    [InlineData("lve", true)]
    [InlineData("zhuang", true)]
    [InlineData("jv", false)]
    [InlineData("Ma", false)]
    [InlineData("ma3", false)]
    [InlineData("", false)]
    public void IsValidKey_ChuanQuyTacR5_1(string key, bool expected) =>
        PinyinSyllable.IsValidKey(key).Should().Be(expected);
}
