using AntFarm.Chinese.Application.Pinyin;
using AntFarm.Chinese.Application.Pinyin.Dtos;
using AntFarm.Chinese.Domain.Pinyin;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Pinyin;

public class SubmitToneDrillRequestValidatorTests
{
    private readonly SubmitToneDrillRequestValidator _validator = new();

    private static SubmitToneDrillItemRequest ListenToneItem(string syllable = "ma", string hanzi = "马", int expected = 3, int answered = 3, int? responseMs = 1500, int replayCount = 0) =>
        new([new SubmitToneDrillPartRequest(syllable, hanzi, expected, answered)], responseMs, replayCount);

    private static SubmitToneDrillRequest ValidListenToneRequest(int itemCount = 20) => new(
        Guid.NewGuid(), ToneDrillMode.ListenTone,
        DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow,
        Enumerable.Range(0, itemCount).Select(_ => ListenToneItem()).ToList());

    [Fact]
    public void Validate_ZeroCau_Loi()
    {
        var request = ValidListenToneRequest(0);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_101Cau_Loi()
    {
        var request = ValidListenToneRequest(101);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_TonePairMotPhan_Loi()
    {
        var request = new SubmitToneDrillRequest(
            Guid.NewGuid(), ToneDrillMode.TonePair, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow,
            [ListenToneItem()]); // tone_pair nhưng chỉ 1 phần

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ToneNgoaiKhoang5_Loi()
    {
        var request = ValidListenToneRequest(1) with
        {
            Items = [ListenToneItem(expected: 5, answered: 3)]
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_TonePair33_Loi()
    {
        var request = new SubmitToneDrillRequest(
            Guid.NewGuid(), ToneDrillMode.TonePair, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow,
            [new SubmitToneDrillItemRequest(
                [new SubmitToneDrillPartRequest("ma", "马", 3, 3), new SubmitToneDrillPartRequest("hao", "好", 3, 2)],
                2000, 0)]);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_HaiPhanCungSyllable_Loi()
    {
        var request = new SubmitToneDrillRequest(
            Guid.NewGuid(), ToneDrillMode.TonePair, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow,
            [new SubmitToneDrillItemRequest(
                [new SubmitToneDrillPartRequest("ma", "马", 1, 1), new SubmitToneDrillPartRequest("ma", "妈", 2, 2)],
                2000, 0)]);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ClientSessionIdRong_Loi()
    {
        var request = ValidListenToneRequest(1) with { ClientSessionId = Guid.Empty };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_StartedAtKhongPhaiUtc_Loi()
    {
        var request = ValidListenToneRequest(1) with
        {
            StartedAt = DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(-5), DateTimeKind.Unspecified)
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    /// <summary>D38: offset SỐ (Kind=Local sau khi System.Text.Json quy đổi) cũng bị từ chối — chỉ 'Z' (Kind=Utc) mới qua.</summary>
    [Fact]
    public void Validate_FinishedAtKindLocal_TuOffsetSo_Loi()
    {
        var request = ValidListenToneRequest(1) with
        {
            FinishedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local)
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    /// <summary>review F5 17/09/2026: "items": [null, ...] không được làm validator ném NullReferenceException (⇒ 500) — phải ra lỗi 400 bình thường.</summary>
    [Fact]
    public void Validate_ItemsChuaPhanTuNull_LoiKhongNem()
    {
        var request = ValidListenToneRequest(1) with
        {
            Items = [null!, ListenToneItem()]
        };

        var act = () => _validator.Validate(request);

        act.Should().NotThrow();
        act().IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_HanziKhongPhaiChuHan_Loi()
    {
        var request = ValidListenToneRequest(1) with
        {
            Items = [ListenToneItem(hanzi: "A")]
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_SyllableSaiDinhDang_Loi()
    {
        var request = ValidListenToneRequest(1) with
        {
            Items = [ListenToneItem(syllable: "MA")]
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_HopLe_ListenTone_Qua()
    {
        var request = ValidListenToneRequest(20);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_HopLe_TonePair_Qua()
    {
        var request = new SubmitToneDrillRequest(
            Guid.NewGuid(), ToneDrillMode.TonePair, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow,
            Enumerable.Range(0, 20).Select(_ => new SubmitToneDrillItemRequest(
                [new SubmitToneDrillPartRequest("ma", "妈", 1, 1), new SubmitToneDrillPartRequest("cha", "茶", 2, 3)],
                2500, 1)).ToList());

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ResponseMsAmHoacQuaLon_Loi()
    {
        var request = ValidListenToneRequest(1) with
        {
            Items = [ListenToneItem(responseMs: 600_001)]
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ReplayCountQuaLon_Loi()
    {
        var request = ValidListenToneRequest(1) with
        {
            Items = [ListenToneItem(replayCount: 101)]
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
