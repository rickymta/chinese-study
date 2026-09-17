using AntFarm.Chinese.Domain.Srs;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Srs;

/// <summary>V8 (§5.2.7) — tính chất bất biến của FSRS-6, chép từ test thuộc tính của py-fsrs (<c>tests/test_basic.py</c>), cộng thêm vài kiểm tra phòng thủ riêng của AntFarm (Kind=Utc, trọng số ngoài cận).</summary>
public class FsrsPropertyTests
{
    private static readonly FsrsScheduler Scheduler = new(FsrsOptions.Default());

    private static SrsMemory NewCard(DateTime createdAtUtc) => new(SrsState.New, null, null, null, createdAtUtc, null);

    /// <summary>Chấm Easy 10 lần liên tiếp cách nhau 1 µs ⇒ độ khó hội tụ về 1.0 (cận dưới).</summary>
    [Fact]
    public void ChamDeLienTuc10Lan_DoKhoHoiTuVe1()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var card = NewCard(now);

        for (var i = 0; i < 10; i++)
        {
            now = now.AddTicks(10); // 1 micro-giây = 10 tick .NET
            var result = Scheduler.Review(card, SrsRating.Easy, now);
            card = result.After;
        }

        card.Difficulty.Should().NotBeNull();
        card.Difficulty!.Value.Should().BeApproximately(1.0, 1e-6);
    }

    /// <summary>Chấm Again 1000 lần liên tiếp, mỗi lần đúng lúc due của lượt trước ⇒ độ ổn định KHÔNG BAO GIỜ dưới cận 0,001 (chống tràn/âm số qua nhiều vòng lặp).</summary>
    [Fact]
    public void ChamQuenLienTuc1000Lan_OnDinhKhongDuoiCanDuoi()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var card = NewCard(now);

        for (var i = 0; i < 1000; i++)
        {
            var result = Scheduler.Review(card, SrsRating.Again, now);
            card = result.After;

            card.Stability.Should().NotBeNull();
            card.Stability!.Value.Should().BeGreaterThanOrEqualTo(0.001);

            now = card.DueAt; // lượt kế chấm đúng lúc due của lượt này
        }
    }

    /// <summary>Mọi thẻ Review: khoảng dự kiến Quên &lt; Khó ≤ Được ≤ Dễ (R7-1 — cơ sở để giao diện xếp 4 nút theo thứ tự tăng dần).</summary>
    [Theory]
    [InlineData(1.5, 3.0, 0)]
    [InlineData(10.0, 5.0, 5)]
    [InlineData(200.0, 8.0, 40)]
    public void Preview_TheReview_ThuTuKhoangTangDan(double stability, double difficulty, int elapsedDays)
    {
        var last = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var card = new SrsMemory(SrsState.Review, null, stability, difficulty, last, last);
        var now = last.AddDays(elapsedDays);

        var preview = Scheduler.Preview(card, now);

        preview[SrsRating.Again].Should().BeLessThan(preview[SrsRating.Hard]);
        preview[SrsRating.Hard].Should().BeLessThanOrEqualTo(preview[SrsRating.Good]);
        preview[SrsRating.Good].Should().BeLessThanOrEqualTo(preview[SrsRating.Easy]);
    }

    /// <summary>Preview KHÔNG làm đổi thẻ: gọi Preview trước hay không không ảnh hưởng kết quả Review kế tiếp (không có trạng thái ẩn nào trong scheduler).</summary>
    [Fact]
    public void Preview_KhongLamDoiKetQuaReviewKeTiep()
    {
        var last = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var card = new SrsMemory(SrsState.Review, null, 10.0, 5.0, last, last);
        var now = last.AddDays(5);

        var expected = Scheduler.Review(card, SrsRating.Good, now);

        _ = Scheduler.Preview(card, now); // gọi Preview cả 4 mức trước
        _ = Scheduler.Preview(card, now); // gọi lần nữa cho chắc — scheduler phải thuần, không có state ẩn

        var actual = Scheduler.Review(card, SrsRating.Good, now);

        actual.Should().Be(expected);
    }

    [Fact]
    public void Review_NowUtcKhongPhaiUtc_NemArgumentException()
    {
        var card = NewCard(DateTime.UtcNow);
        var localNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local);
        var unspecifiedNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        var actLocal = () => Scheduler.Review(card, SrsRating.Good, localNow);
        var actUnspecified = () => Scheduler.Review(card, SrsRating.Good, unspecifiedNow);

        actLocal.Should().Throw<ArgumentException>();
        actUnspecified.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Preview_NowUtcKhongPhaiUtc_NemArgumentException()
    {
        var card = NewCard(DateTime.UtcNow);
        var localNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local);

        var act = () => Scheduler.Preview(card, localNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Retrievability_NowUtcKhongPhaiUtc_NemArgumentException()
    {
        var last = DateTime.UtcNow;
        var card = new SrsMemory(SrsState.Review, null, 10.0, 5.0, last, last);
        var localNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local);

        var act = () => Scheduler.Retrievability(card, localNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Retrievability_TheChuaOnLanNao_TraVe0()
    {
        var card = NewCard(DateTime.UtcNow);

        Scheduler.Retrievability(card, DateTime.UtcNow).Should().Be(0);
    }

    [Fact]
    public void KhoiTao_SaiSoLuongTrongSo_Nem()
    {
        var options = FsrsOptions.Default() with { Weights = [0.1, 0.2] };

        var act = () => new FsrsScheduler(options);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void KhoiTao_TrongSoNgoaiCan_Nem()
    {
        var badWeights = FsrsOptions.DefaultWeights.ToArray();
        badWeights[4] = 0.5; // cận dưới w4 là 1.0 (§5.2.6 D0 ∈ [1,10])
        var options = FsrsOptions.Default() with { Weights = badWeights };

        var act = () => new FsrsScheduler(options);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void KhoiTao_TrongSoMacDinh_KhongNem()
    {
        var act = () => new FsrsScheduler(FsrsOptions.Default());

        act.Should().NotThrow();
    }
}
