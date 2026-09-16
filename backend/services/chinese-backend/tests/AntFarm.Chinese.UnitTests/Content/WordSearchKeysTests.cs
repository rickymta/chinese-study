using AntFarm.Chinese.Domain.Content;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Content;

/// <summary>§5.2.2 — ví dụ 爱 khớp từng ký tự.</summary>
public class WordSearchKeysTests
{
    [Theory]
    [InlineData("ai4", "ai4")]
    [InlineData("Bei3 jing1", "bei3jing1")]
    [InlineData("nv3", "nv3")]
    public void PinyinCompact_DungCongThuc(string pinyin, string expected) =>
        WordSearchKeys.PinyinCompact(pinyin).Should().Be(expected);

    [Theory]
    [InlineData("ai4", "ai")]
    [InlineData("Bei3 jing1", "beijing")]
    public void PinyinToneless_DungCongThuc(string pinyin, string expected) =>
        WordSearchKeys.PinyinToneless(pinyin).Should().Be(expected);

    [Fact]
    public void BuildSearchVi_ViDuAiTheoHopDong()
    {
        var meaningsVi = new[]
        {
            "yêu; thích",
            "tình cảm",
            "có khuynh hướng (làm gì đó); có xu hướng (xảy ra)"
        };

        var (withDiacritics, plain) = WordSearchKeys.BuildSearchVi(meaningsVi, "ái");

        withDiacritics.Should().Be("| yêu | thích | tình cảm | có khuynh hướng | có xu hướng | ái |");
        plain.Should().Be("| yeu | thich | tinh cam | co khuynh huong | co xu huong | ai |");
    }

    [Fact]
    public void BuildSearchVi_HanVietRong_KhongThemTerm()
    {
        var (withDiacritics, _) = WordSearchKeys.BuildSearchVi(["tốt"], null);
        withDiacritics.Should().Be("| tốt |");
    }
}
