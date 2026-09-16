using AntFarm.Chinese.Domain.Pinyin;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Pinyin;

/// <summary>§5.2.5 — kiểm PinyinQuery bằng bảng âm tiết THẬT nhúng cứng (PinyinSyllableTable, mặc định của PinyinQuery).</summary>
public class PinyinQueryTests
{
    [Theory]
    [InlineData("ni3hao3")]
    [InlineData("ni3 hao3")]
    [InlineData("NI3HAO3")] // TryParseNumbered kỳ vọng đầu vào ĐÃ lower (giống cách DictionaryQueryParser gọi) — test tự lower trước khi gọi.
    public void TryParseNumbered_PhanTichDungPinyinSo(string input)
    {
        var l = input.ToLowerInvariant();
        var ok = PinyinQuery.TryParseNumbered(l, out var compact);
        ok.Should().BeTrue();
        compact.Should().Be("ni3hao3");
    }

    [Fact]
    public void TryParseNumbered_Ma0_ThanhNheChuyenThanh5()
    {
        PinyinQuery.TryParseNumbered("ma0", out var compact).Should().BeTrue();
        compact.Should().Be("ma5");
    }

    [Fact]
    public void TryParseNumbered_ChuKhongPhaiAmTietThat_KhongHopLe() =>
        PinyinQuery.TryParseNumbered("abc1", out _).Should().BeFalse();

    [Fact]
    public void TryParseNumbered_ThieuSoOMotAmTiet_KhongHopLe() =>
        // "ni3hao" — chỉ "ni3" có số, "hao" không ⇒ không khớp định dạng đầy đủ.
        PinyinQuery.TryParseNumbered("ni3hao", out _).Should().BeFalse();

    [Theory]
    [InlineData("nǐhǎo")]
    [InlineData("nǐ hǎo")]
    [InlineData("nǐ'hǎo")]
    public void TryParseToneMarked_NiHaoLien_TachDungHaiAmTiet(string input) =>
        RunToneMarked(input, "ni3hao3");

    [Fact]
    public void TryParseToneMarked_XiAnLien_TachDungHaiAmTietKhongNhamLanVoiXian() =>
        // "xīān" (西安 — 2 âm tiết, mỗi âm tiết một dấu) KHÁC "xiān" (先 — 1 âm tiết, chỉ một dấu trên 'a').
        RunToneMarked("xīān", "xi1an1");

    [Fact]
    public void TryParseToneMarked_Ai4_DauHuyền() => RunToneMarked("ài", "ai4");

    [Fact]
    public void TryParseToneMarked_Ai2_DauSac() => RunToneMarked("ái", "ai2");

    [Fact]
    public void TryParseToneMarked_Lu4_UMoc() => RunToneMarked("lǜ", "lv4");

    [Fact]
    public void TryParseToneMarked_KhongCoDauThanh_KhongHopLe() =>
        PinyinQuery.TryParseToneMarked("xian", out _).Should().BeFalse();

    [Fact]
    public void TryParseToneMarked_TiengVietKhongPhaiPinyin_KhongCoKhoa() =>
        // "yêu" có dấu tiếng Việt (ê) nhưng KHÔNG phải dấu thanh pinyin — không khớp bộ ký tự cho phép.
        PinyinQuery.TryParseToneMarked("yêu", out _).Should().BeFalse();

    [Theory]
    [InlineData("xian", "xian")]
    [InlineData("ni hao", "nihao")]
    [InlineData("ni3hao", "nihao")] // Toneless() luôn bỏ số — không phụ thuộc gate của DictionaryQueryParser
    public void Toneless_BoCachVaSo(string input, string expected) =>
        PinyinQuery.Toneless(input).Should().Be(expected);

    private static void RunToneMarked(string input, string expectedCompact)
    {
        var ok = PinyinQuery.TryParseToneMarked(input.ToLowerInvariant(), out var compact);
        ok.Should().BeTrue();
        compact.Should().Be(expectedCompact);
    }
}
