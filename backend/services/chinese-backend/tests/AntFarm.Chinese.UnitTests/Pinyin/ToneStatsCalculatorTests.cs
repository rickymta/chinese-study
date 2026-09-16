using AntFarm.Chinese.Application.Pinyin;
using AntFarm.Chinese.Application.Pinyin.Dtos;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Pinyin;

public class ToneStatsCalculatorTests
{
    [Fact]
    public void Calculate_KhongDuLieu_TraVeBonKhoaTotalKhongAccuracyNullFocusRong()
    {
        var result = ToneStatsCalculator.Calculate([], totalAnswered: 0, sessionsCount: 0, lastSessionAt: null);

        result.ByTone.Should().HaveCount(4);
        foreach (var tone in result.ByTone.Values)
        {
            tone.Total.Should().Be(0);
            tone.Correct.Should().Be(0);
            tone.Accuracy.Should().BeNull();
        }

        result.RecommendedFocus.Should().BeEmpty();
        result.Confusions.Should().BeEmpty();
        result.Accuracy.Should().BeNull();
        result.G0Reached.Should().BeFalse();
        result.TotalAnswered.Should().Be(0);
        result.SessionsCount.Should().Be(0);
        result.LastSessionAt.Should().BeNull();
    }

    [Fact]
    public void Calculate_9CauAccuracy05_KhongVaoFocus_DoTotalDuoi10()
    {
        var rows = BuildRows(expectedTone: 2, total: 9, correctCount: 4); // ~0,44 < 0,8 nhưng total < 10

        var result = ToneStatsCalculator.Calculate(rows, totalAnswered: 9, sessionsCount: 1, lastSessionAt: null);

        result.RecommendedFocus.Should().NotContain(2);
    }

    [Fact]
    public void Calculate_10CauAccuracy07_VaoFocus()
    {
        var rows = BuildRows(expectedTone: 3, total: 10, correctCount: 7); // 0,7 < 0,8 và total >= 10

        var result = ToneStatsCalculator.Calculate(rows, totalAnswered: 10, sessionsCount: 1, lastSessionAt: null);

        result.RecommendedFocus.Should().ContainSingle().Which.Should().Be(3);
        result.ByTone["3"].Total.Should().Be(10);
        result.ByTone["3"].Correct.Should().Be(7);
        result.ByTone["3"].Accuracy.Should().BeApproximately(0.7, 0.0001);
    }

    [Fact]
    public void Calculate_HaiThanhCungYeu_SapDungTheoAccuracyRoiSoThanh()
    {
        var rows = new List<ToneAnswerRow>();
        rows.AddRange(BuildRows(1, 10, 5)); // accuracy 0,5
        rows.AddRange(BuildRows(2, 10, 5)); // accuracy 0,5 — bằng thanh 1, sắp theo số thanh tăng dần
        rows.AddRange(BuildRows(3, 10, 9)); // accuracy 0,9 — không vào focus

        var result = ToneStatsCalculator.Calculate(rows, totalAnswered: rows.Count, sessionsCount: 1, lastSessionAt: null);

        result.RecommendedFocus.Should().Equal(1, 2);
    }

    [Fact]
    public void Calculate_Confusions_ToiDa5VaSapDungTheoCountRoiExpectedRoiAnswered()
    {
        var rows = new List<ToneAnswerRow>
        {
            new(2, 3, false), new(2, 3, false), new(2, 3, false), // count 3
            new(3, 2, false), new(3, 2, false),                   // count 2
            new(1, 4, false),                                     // count 1
            new(4, 1, false),                                     // count 1
            new(1, 2, false),                                     // count 1
            new(2, 4, false),                                     // count 1
            new(1, 1, true)                                       // đúng — không tính vào confusions
        };

        var result = ToneStatsCalculator.Calculate(rows, totalAnswered: rows.Count, sessionsCount: 1, lastSessionAt: null);

        result.Confusions.Should().HaveCount(5);
        result.Confusions[0].Should().Be(new ConfusionDto(2, 3, 3));
        result.Confusions[1].Should().Be(new ConfusionDto(3, 2, 2));
        // Bốn cặp còn lại đều count=1 — sắp theo Expected rồi Answered tăng dần, lấy 3 trong 4.
        result.Confusions.Skip(2).Should().BeInAscendingOrder(c => c.Expected).And.HaveCount(3);
    }

    [Theory]
    [InlineData(19, 17, false)] // thiếu 1 câu để đạt "xong G0" (accuracy 17/19 ≈ 0,895 nhưng total < 20)
    [InlineData(20, 16, false)] // đủ 20 câu nhưng accuracy 0,8 < 0,85
    [InlineData(20, 17, true)] // đúng biên: 20 câu, accuracy 0,85
    public void Calculate_G0Reached_BienDung(int totalPerTone, int correctPerTone, bool expectedG0)
    {
        var rows = new List<ToneAnswerRow>();
        for (var tone = 1; tone <= 4; tone++)
            rows.AddRange(BuildRows(tone, totalPerTone, correctPerTone));

        var result = ToneStatsCalculator.Calculate(rows, totalAnswered: rows.Count, sessionsCount: 1, lastSessionAt: null);

        result.G0Reached.Should().Be(expectedG0);
    }

    [Fact]
    public void Calculate_AccuracyTong_LaTiLeTrenHopBonCuaSo()
    {
        var rows = new List<ToneAnswerRow>();
        rows.AddRange(BuildRows(1, 10, 10));
        rows.AddRange(BuildRows(2, 10, 0));

        var result = ToneStatsCalculator.Calculate(rows, totalAnswered: rows.Count, sessionsCount: 1, lastSessionAt: null);

        result.Accuracy.Should().BeApproximately(0.5, 0.0001);
    }

    private static List<ToneAnswerRow> BuildRows(int expectedTone, int total, int correctCount)
    {
        var rows = new List<ToneAnswerRow>();
        for (var i = 0; i < total; i++)
            rows.Add(new ToneAnswerRow(expectedTone, expectedTone, i < correctCount));
        return rows;
    }
}
