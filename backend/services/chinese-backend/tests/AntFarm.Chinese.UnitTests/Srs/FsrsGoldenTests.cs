using System.Globalization;
using AntFarm.Chinese.Domain.Srs;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Srs;

/// <summary>
/// Vector vàng FSRS-6 — CHÉP NGUYÊN từ hợp đồng
/// <c>docs/agent-workflow/2026-09-17-antfarm-f6-f7-chi-tiet.md</c> §5.2.7 (V1-V8), rồi ĐỐI CHIẾU
/// LẠI bằng cách cài trực tiếp <c>fsrs==6.3.2</c> (py-fsrs, commit
/// <c>9446cb06605c597a063aeee49f7d188d42e34dc2</c>) qua <c>uv run --python 3.12</c> và chạy song
/// song script <c>gold.py</c> (không commit — script tạm, xem bàn giao) — TOÀN BỘ số liệu dưới
/// đây khớp tuyệt đối (không chỉ trong 1e-6) với đầu ra thật của thư viện Python.
/// Dung sai so sánh: S/D/R tuyệt đối ≤ 1e-6 (R7-1, py-fsrs tự kiểm 1e-4 — ta chặt hơn vì cùng
/// phép tính <c>double</c>); <c>due_at</c> so bằng tuyệt đối.
/// </summary>
public class FsrsGoldenTests
{
    private const double Tolerance = 1e-6;

    private static readonly FsrsScheduler Scheduler = new(FsrsOptions.Default());

    private static DateTime Utc(string iso) =>
        DateTime.Parse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    private static SrsMemory NewCard(DateTime createdAtUtc) =>
        new(SrsState.New, null, null, null, createdAtUtc, null);

    private static void AssertAfter(
        SrsMemory after, SrsState state, int? step, double stability, double difficulty, DateTime dueAtUtc)
    {
        after.State.Should().Be(state);
        after.Step.Should().Be(step);
        after.Stability.Should().NotBeNull();
        after.Stability!.Value.Should().BeApproximately(stability, Tolerance);
        after.Difficulty.Should().NotBeNull();
        after.Difficulty!.Value.Should().BeApproximately(difficulty, Tolerance);
        after.DueAt.Kind.Should().Be(DateTimeKind.Utc);
        after.DueAt.Should().Be(dueAtUtc);
    }

    /// <summary>V1 — thẻ mới, mỗi lượt chấm đúng lúc <c>due</c> của lượt trước, G = 3,3,3,3,3,3,1,1,3,3,3,3,3.</summary>
    [Fact]
    public void V1_ChuoiTraLoiDungHan_KhopVectorVang()
    {
        var start = Utc("2022-11-29T12:30:00Z");
        var card = NewCard(start);
        var now = start;

        (SrsRating G, SrsState State, int? Step, double S, double D, string Due)[] rows =
        [
            (SrsRating.Good, SrsState.Learning, 1, 2.3065, 2.118103970459016, "2022-11-29T12:40:00Z"),
            (SrsRating.Good, SrsState.Review, null, 2.3065, 2.111214235785395, "2022-12-01T12:40:00Z"),
            (SrsRating.Good, SrsState.Review, null, 10.971048263078135, 2.1043313908464483, "2022-12-12T12:40:00Z"),
            (SrsRating.Good, SrsState.Review, null, 46.316858440073425, 2.0974554287524403, "2023-01-27T12:40:00Z"),
            (SrsRating.Good, SrsState.Review, null, 162.99981577472244, 2.0905863426205262, "2023-07-09T12:40:00Z"),
            (SrsRating.Good, SrsState.Review, null, 497.8765551245907, 2.083724125574744, "2024-11-18T12:40:00Z"),
            (SrsRating.Again, SrsState.Relearning, 0, 6.890412507565338, 7.383202320049203, "2024-11-18T12:50:00Z"),
            (SrsRating.Again, SrsState.Relearning, 0, 2.154598374301973, 9.125104766121234, "2024-11-18T13:00:00Z"),
            (SrsRating.Good, SrsState.Review, null, 2.154598374301973, 9.11120803065195, "2024-11-20T13:00:00Z"),
            (SrsRating.Good, SrsState.Review, null, 3.9831233795187773, 9.097325191918136, "2024-11-24T13:00:00Z"),
            (SrsRating.Good, SrsState.Review, null, 7.236254476883319, 9.083456236023055, "2024-12-01T13:00:00Z"),
            (SrsRating.Good, SrsState.Review, null, 12.483043578674472, 9.06960114908387, "2024-12-13T13:00:00Z"),
            (SrsRating.Good, SrsState.Review, null, 20.77035728499242, 9.055759917231622, "2025-01-03T13:00:00Z")
        ];

        foreach (var row in rows)
        {
            var result = Scheduler.Review(card, row.G, now);
            AssertAfter(result.After, row.State, row.Step, row.S, row.D, Utc(row.Due));
            card = result.After;
            now = card.DueAt;
        }
    }

    /// <summary>V2 — mỗi lượt cộng thêm số ngày ở cột "+ngày" vào ĐỒNG HỒ CHẤM (không theo due).</summary>
    [Fact]
    public void V2_CongDonNgayVaoThoiDiemCham_KhopVectorVang()
    {
        var now = Utc("2022-11-29T12:30:00Z");
        var card = NewCard(now);

        (SrsRating G, int PlusDays, SrsState State, int? Step, double S, double D, string Due)[] rows =
        [
            (SrsRating.Again, 0, SrsState.Learning, 0, 0.212, 6.4133, "2022-11-29T12:31:00Z"),
            (SrsRating.Good, 0, SrsState.Learning, 1, 0.24668918777567272, 6.402115069296838, "2022-11-29T12:40:00Z"),
            (SrsRating.Good, 1, SrsState.Review, null, 2.021477251638192, 6.3909413235243795, "2022-12-02T12:30:00Z"),
            (SrsRating.Good, 3, SrsState.Review, null, 7.863698841006282, 6.379778751497693, "2022-12-11T12:30:00Z"),
            (SrsRating.Good, 8, SrsState.Review, null, 21.917713153152434, 6.368627342043034, "2023-01-02T12:30:00Z"),
            (SrsRating.Good, 21, SrsState.Review, null, 53.626902917141365, 6.357487083997829, "2023-02-24T12:30:00Z")
        ];

        foreach (var row in rows)
        {
            now = now.AddDays(row.PlusDays);
            var result = Scheduler.Review(card, row.G, now);
            AssertAfter(result.After, row.State, row.Step, row.S, row.D, Utc(row.Due));
            card = result.After;
        }
    }

    /// <summary>V3 — lượt đầu của thẻ mới (khoảng dự kiến trên 4 nút của thẻ mới).</summary>
    [Theory]
    [InlineData(SrsRating.Again, SrsState.Learning, 0, 0.212, 6.4133, "2026-09-17T01:01:00Z")]
    [InlineData(SrsRating.Hard, SrsState.Learning, 0, 1.2931, 5.112170705601056, "2026-09-17T01:05:30Z")]
    [InlineData(SrsRating.Good, SrsState.Learning, 1, 2.3065, 2.118103970459016, "2026-09-17T01:10:00Z")]
    [InlineData(SrsRating.Easy, SrsState.Review, null, 8.2956, 1.0, "2026-09-25T01:00:00Z")]
    public void V3_TheMoi_LuotDau(SrsRating g, SrsState state, int? step, double s, double d, string due)
    {
        var start = Utc("2026-09-17T01:00:00Z");
        var card = NewCard(start);

        var result = Scheduler.Review(card, g, start);

        AssertAfter(result.After, state, step, s, d, Utc(due));
    }

    private static SrsMemory BuildV4Base(out DateTime dueAtBase)
    {
        var start = Utc("2026-09-17T01:00:00Z");
        var card = NewCard(start);

        var first = Scheduler.Review(card, SrsRating.Good, start);
        var second = Scheduler.Review(first.After, SrsRating.Good, first.After.DueAt);

        // Nền dùng chung cho V4/V5 (§5.2.7): review, S=2.3065, D=2.111214235785395, due 2026-09-19T01:10Z.
        AssertAfter(second.After, SrsState.Review, null, 2.3065, 2.111214235785395, Utc("2026-09-19T01:10:00Z"));

        dueAtBase = second.After.DueAt;
        return second.After;
    }

    /// <summary>V4(a) — chấm đúng hạn từ nền Review (S=2.3065, D=2.111214235785395, due 2026-09-19T01:10Z).</summary>
    [Theory]
    [InlineData(SrsRating.Again, SrsState.Relearning, 0, 0.6077016626638644, 7.392238132342694, "2026-09-19T01:20:00Z")]
    [InlineData(SrsRating.Hard, SrsState.Review, null, 7.517359325415191, 4.748284761594571, "2026-09-27T01:10:00Z")]
    [InlineData(SrsRating.Good, SrsState.Review, null, 10.971048263078135, 2.1043313908464483, "2026-09-30T01:10:00Z")]
    [InlineData(SrsRating.Easy, SrsState.Review, null, 18.534332441919037, 1.0, "2026-10-08T01:10:00Z")]
    public void V4a_TuNenReview_ChamDungHan(SrsRating g, SrsState state, int? step, double s, double d, string due)
    {
        var baseCard = BuildV4Base(out var dueAtBase);

        var result = Scheduler.Review(baseCard, g, dueAtBase);

        AssertAfter(result.After, state, step, s, d, Utc(due));
    }

    /// <summary>V4(b) — chấm trễ 3 ngày từ cùng nền Review.</summary>
    [Theory]
    [InlineData(SrsRating.Again, SrsState.Relearning, 0, 0.6827348149809292, 7.392238132342694, "2026-09-22T01:20:00Z")]
    [InlineData(SrsRating.Hard, SrsState.Review, null, 11.852915497576285, 4.748284761594571, "2026-10-04T01:10:00Z")]
    [InlineData(SrsRating.Good, SrsState.Review, null, 18.180153970030403, 2.1043313908464483, "2026-10-10T01:10:00Z")]
    [InlineData(SrsRating.Easy, SrsState.Review, null, 32.03626652046994, 1.0, "2026-10-24T01:10:00Z")]
    public void V4b_TuNenReview_ChamTre3Ngay(SrsRating g, SrsState state, int? step, double s, double d, string due)
    {
        var baseCard = BuildV4Base(out var dueAtBase);
        var lateNow = dueAtBase.AddDays(3);

        var result = Scheduler.Review(baseCard, g, lateNow);

        AssertAfter(result.After, state, step, s, d, Utc(due));
    }

    /// <summary>V5 — từ V4(a) G=Again (relearning, step 0, due 2026-09-19T01:20Z), chấm đúng hạn.</summary>
    [Theory]
    [InlineData(SrsRating.Again, SrsState.Relearning, 0, 0.22294679606919743, 9.128074776178583, "2026-09-19T01:30:00Z")]
    [InlineData(SrsRating.Hard, SrsState.Relearning, 0, 0.6077016626638644, 8.254074519842886, "2026-09-19T01:35:00Z")]
    [InlineData(SrsRating.Good, SrsState.Review, null, 0.6597976257475758, 7.38007426350719, "2026-09-20T01:20:00Z")]
    [InlineData(SrsRating.Easy, SrsState.Review, null, 1.1350513376989504, 6.5060740071714935, "2026-09-20T01:20:00Z")]
    public void V5_TuRelearning_ChamDungHan(SrsRating g, SrsState state, int? step, double s, double d, string due)
    {
        var baseCard = BuildV4Base(out var dueAtBase);
        var v4A = Scheduler.Review(baseCard, SrsRating.Again, dueAtBase);
        AssertAfter(v4A.After, SrsState.Relearning, 0, 0.6077016626638644, 7.392238132342694, Utc("2026-09-19T01:20:00Z"));

        var result = Scheduler.Review(v4A.After, g, v4A.After.DueAt);

        AssertAfter(result.After, state, step, s, d, Utc(due));
    }

    /// <summary>V6 — khả năng nhớ R với thẻ review S=10, last=2026-09-17T01:00Z.</summary>
    [Theory]
    [InlineData(0, 1.0)]
    [InlineData(1, 0.9856824087775146)]
    [InlineData(10, 0.9)] // = R(S,S) — tính chất định nghĩa của FACTOR/DECAY ở độ nhớ mục tiêu mặc định 0,9.
    [InlineData(30, 0.8093881035731708)]
    [InlineData(100, 0.6928266345726217)]
    public void V6_KhaNangNho(int t, double expectedR)
    {
        var last = Utc("2026-09-17T01:00:00Z");
        var card = new SrsMemory(SrsState.Review, null, 10.0, 5.0, last, last);

        var r = Scheduler.Retrievability(card, last.AddDays(t));

        r.Should().BeApproximately(expectedR, Tolerance);
    }

    /// <summary>V7 — khoảng ôn I(S) và D0(G) — kiểm trực tiếp công thức thuần, không qua máy trạng thái.</summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2.3065, 2)]
    [InlineData(10, 10)]
    [InlineData(100, 100)]
    public void V7_KhoangOn_DoNhoMacDinh(double stability, int expectedDays)
    {
        Scheduler.NextIntervalDays(stability).Should().Be(expectedDays);
    }

    [Theory]
    [InlineData(0.80, 33)]
    [InlineData(0.85, 19)]
    [InlineData(0.95, 4)]
    [InlineData(0.97, 2)]
    public void V7_KhoangOn_TheoDoNhoMucTieu(double desiredRetention, int expectedDays)
    {
        var scheduler = new FsrsScheduler(FsrsOptions.Default(desiredRetention));

        scheduler.NextIntervalDays(10).Should().Be(expectedDays);
    }

    [Theory]
    [InlineData(SrsRating.Again, 6.4133)]
    [InlineData(SrsRating.Hard, 5.112170705601056)]
    [InlineData(SrsRating.Good, 2.118103970459016)]
    [InlineData(SrsRating.Easy, 1.0)]
    public void V7_D0_TheoRating(SrsRating rating, double expectedD0)
    {
        Scheduler.InitialDifficulty(rating, clamp: true).Should().BeApproximately(expectedD0, Tolerance);
    }
}
