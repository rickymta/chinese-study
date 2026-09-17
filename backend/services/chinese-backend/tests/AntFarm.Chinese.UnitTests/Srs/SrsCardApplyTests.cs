using AntFarm.Chinese.Domain.Srs;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Srs;

/// <summary>§5.2.9 — <c>SrsCard.Apply</c> (lapses, first_reviewed_*, chuyển new→learning) ĐỘC LẬP với FSRS thật (dùng thẳng <see cref="FsrsScheduler"/> vector V1 để có kết quả tất định).</summary>
public class SrsCardApplyTests
{
    private static readonly FsrsScheduler Scheduler = new(FsrsOptions.Default());

    private static SrsCard NewCard(DateTime createdAtUtc) => SrsCard.CreateNew(Guid.NewGuid(), Guid.NewGuid(), SrsCardSources.Path, createdAtUtc);

    [Fact]
    public void TheMoi_ChamMotLan_ChuyenSangLearning()
    {
        var now = new DateTime(2026, 9, 17, 1, 0, 0, DateTimeKind.Utc);
        var card = NewCard(now);

        var result = Scheduler.Review(card.ToMemory(), SrsRating.Good, now);
        card.Apply(result, SrsRating.Good, now, new DateOnly(2026, 9, 17));

        card.State.Should().Be(SrsState.Learning);
        card.Step.Should().Be(1);
        card.Reps.Should().Be(1);
        card.Lapses.Should().Be(0);
    }

    [Fact]
    public void FirstReviewedAt_ChiDatLanDauTien_KhongBiGhiDeLuotSau()
    {
        var start = new DateTime(2026, 9, 17, 1, 0, 0, DateTimeKind.Utc);
        var card = NewCard(start);

        var first = Scheduler.Review(card.ToMemory(), SrsRating.Good, start);
        card.Apply(first, SrsRating.Good, start, new DateOnly(2026, 9, 17));

        card.FirstReviewedAt.Should().Be(start);
        card.FirstReviewedLocalDate.Should().Be(new DateOnly(2026, 9, 17));

        var laterAt = start.AddMinutes(10);
        var second = Scheduler.Review(card.ToMemory(), SrsRating.Good, laterAt);
        card.Apply(second, SrsRating.Good, laterAt, new DateOnly(2026, 9, 18));

        // Lượt hai xảy ra "ngày khác" (giả lập) nhưng KHÔNG được ghi đè mốc lần đầu.
        card.FirstReviewedAt.Should().Be(start);
        card.FirstReviewedLocalDate.Should().Be(new DateOnly(2026, 9, 17));
        card.Reps.Should().Be(2);
    }

    [Fact]
    public void Lapses_TangKhiDangReviewMaChamQuen()
    {
        var start = new DateTime(2026, 9, 17, 1, 0, 0, DateTimeKind.Utc);
        var card = NewCard(start);

        // Đưa thẻ về Review trước (Good rồi Good, giống V1 hàng 0-1 của FsrsGoldenTests).
        var r1 = Scheduler.Review(card.ToMemory(), SrsRating.Good, start);
        card.Apply(r1, SrsRating.Good, start, DateOnly.FromDateTime(start));
        var r2 = Scheduler.Review(card.ToMemory(), SrsRating.Good, card.DueAt);
        card.Apply(r2, SrsRating.Good, card.DueAt, DateOnly.FromDateTime(card.DueAt));
        card.State.Should().Be(SrsState.Review);
        card.Lapses.Should().Be(0);

        var beforeAgain = card.DueAt;
        var r3 = Scheduler.Review(card.ToMemory(), SrsRating.Again, beforeAgain);
        card.Apply(r3, SrsRating.Again, beforeAgain, DateOnly.FromDateTime(beforeAgain));

        card.State.Should().Be(SrsState.Relearning);
        card.Lapses.Should().Be(1);
    }

    [Fact]
    public void Lapses_KhongTangKhiTheMoiChamQuen()
    {
        var start = new DateTime(2026, 9, 17, 1, 0, 0, DateTimeKind.Utc);
        var card = NewCard(start);

        var result = Scheduler.Review(card.ToMemory(), SrsRating.Again, start);
        card.Apply(result, SrsRating.Again, start, DateOnly.FromDateTime(start));

        // Thẻ MỚI (chưa từng ở Review) chấm "Quên" ⇒ KHÔNG tính là lapse (R7-11: chỉ tăng khi
        // state_before = review).
        card.Lapses.Should().Be(0);
        card.State.Should().Be(SrsState.Learning);
    }

    [Fact]
    public void SetSuspended_DatCoTamDungVaUpdatedAt()
    {
        var now = new DateTime(2026, 9, 17, 1, 0, 0, DateTimeKind.Utc);
        var card = NewCard(now);

        var later = now.AddDays(1);
        card.SetSuspended(true, later);

        card.IsSuspended.Should().BeTrue();
        card.UpdatedAt.Should().Be(later);
    }
}
