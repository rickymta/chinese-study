using AntFarm.Chinese.Application.Writing;
using AntFarm.Chinese.Application.Writing.Dtos;
using AntFarm.Chinese.Application.Writing.Validators;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Writing;

/// <summary>§5.2.2 test — <c>hanzi</c> phải đúng MỘT chữ Hán (kể cả chữ mở rộng B, surrogate pair).</summary>
public class RecordWritingAttemptValidatorTests
{
    private readonly RecordWritingAttemptValidator _validator = new();

    private static RecordWritingAttemptRequest Valid(string hanzi = "爱", string mode = "recall") =>
        new(Guid.NewGuid(), hanzi, mode, 10, 0, 0, 5000);

    [Fact]
    public void MotChuHanDon_HopLe()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void HaiChu_Loi()
    {
        _validator.Validate(Valid(hanzi: "爱我")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void KyTuLatin_Loi()
    {
        _validator.Validate(Valid(hanzi: "a")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ChuoiRong_Loi()
    {
        _validator.Validate(Valid(hanzi: "")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ChuMoRongB_SurrogatePair_HopLe()
    {
        // U+20000 (𠀀) — chữ mở rộng B, biểu diễn bằng MỘT cặp surrogate trong UTF-16 (Rune đếm là 1 code point).
        _validator.Validate(Valid(hanzi: "𠀀")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("guided")]
    [InlineData("recall")]
    public void ModeHopLe(string mode) => _validator.Validate(Valid(mode: mode)).IsValid.Should().BeTrue();

    [Fact]
    public void ModeLa_ModeKhac_Loi() => _validator.Validate(Valid(mode: "xem")).IsValid.Should().BeFalse();

    [Fact]
    public void ClientAttemptIdRong_Loi()
    {
        var request = Valid() with { ClientAttemptId = Guid.Empty };
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(64, true)]
    [InlineData(65, false)]
    public void TotalStrokes_Bien(int strokes, bool expectedValid)
    {
        var request = Valid() with { TotalStrokes = strokes };
        _validator.Validate(request).IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void TotalMistakes_Bien(int mistakes, bool expectedValid)
    {
        var request = Valid() with { TotalMistakes = mistakes };
        _validator.Validate(request).IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void HintsUsed_Bien(int hints, bool expectedValid)
    {
        var request = Valid() with { HintsUsed = hints };
        _validator.Validate(request).IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void DurationMsAmVoCung_HopLe()
    {
        var request = Valid() with { DurationMs = null };
        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void DurationMsQuaLon_Loi()
    {
        var request = Valid() with { DurationMs = 3_600_001 };
        _validator.Validate(request).IsValid.Should().BeFalse();
    }
}

/// <summary>§5.2.2 test — chữ mở rộng B (surrogate pair) chỉ hợp lệ nếu ĐÚNG một code point thuộc khối Ext B.</summary>
public class CjkCharacterValidationTests
{
    [Theory]
    [InlineData("爱", true)]
    [InlineData("", false)]
    [InlineData("爱我", false)]
    [InlineData("a", false)]
    [InlineData("1", false)]
    [InlineData(" ", false)]
    public void KyTuDon(string text, bool expected) => CjkCharacterValidation.IsSingleCjkCharacter(text).Should().Be(expected);

    [Fact]
    public void ChuMoRongB_MotCodePoint_HopLe() =>
        CjkCharacterValidation.IsSingleCjkCharacter("𠀀").Should().BeTrue(); // U+20000

    [Fact]
    public void SurrogatePairKhongHopLe_TinhLaKhongHopLe() =>
        // Surrogate cao đứng một mình (không ghép đôi) — không phải một Rune hợp lệ.
        CjkCharacterValidation.IsSingleCjkCharacter("\uD840").Should().BeFalse();
}
