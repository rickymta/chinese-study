using AntFarm.Chinese.Domain.Learning;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Learning;

/// <summary>§5.2.2 test — R-W5 "thuộc chữ" (mastered) và "cần luyện" (weak).</summary>
public class CharacterWritingStatsTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 9, 17, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Day1 = new(2026, 9, 17);
    private static readonly DateOnly Day2 = new(2026, 9, 18);

    private static CharacterWritingStats NewStats() => CharacterWritingStats.CreateEmpty(UserId, "爱", Now);

    private static WritingAttempt Recall(int mistakes = 0, int hints = 0, DateTime? at = null) =>
        WritingAttempt.Create(Guid.NewGuid(), UserId, "爱", WritingModes.Recall, 10, mistakes, hints, null, at ?? Now, Day1);

    private static WritingAttempt Guided(int mistakes = 0, int hints = 0) =>
        WritingAttempt.Create(Guid.NewGuid(), UserId, "爱", WritingModes.Guided, 10, mistakes, hints, null, Now, Day1);

    [Fact]
    public void ChuaVietLanNao_LaNew()
    {
        var stats = NewStats();

        stats.MasteryStatus.Should().Be(MasteryStatuses.New);
        stats.IsWeak.Should().BeFalse();
    }

    [Fact]
    public void HaiLanRecallSach_CungNgay_CleanRecallDaysChiTang1_VanPracticing()
    {
        var stats = NewStats();

        stats.Apply(Recall(), Day1);
        stats.Apply(Recall(), Day1); // lần sạch THỨ HAI trong CÙNG ngày — không tăng (R-W5)

        stats.CleanRecallDays.Should().Be(1);
        stats.MasteryStatus.Should().Be(MasteryStatuses.Practicing);
        stats.Attempts.Should().Be(2);
        stats.RecallAttempts.Should().Be(2);
    }

    [Fact]
    public void HaiLanRecallSach_NgayKhacNhau_CleanRecallDaysLa2_Mastered()
    {
        var stats = NewStats();

        stats.Apply(Recall(), Day1);
        stats.Apply(Recall(), Day2);

        stats.CleanRecallDays.Should().Be(2);
        stats.MasteryStatus.Should().Be(MasteryStatuses.Mastered);
    }

    [Fact]
    public void RecallCoGoiY_KhongTinhSach_KhongTangCleanRecallDays()
    {
        var stats = NewStats();

        stats.Apply(Recall(mistakes: 0, hints: 1), Day1);
        stats.Apply(Recall(mistakes: 0, hints: 1), Day2);

        stats.CleanRecallDays.Should().Be(0);
        stats.MasteryStatus.Should().Be(MasteryStatuses.Practicing);
    }

    [Fact]
    public void RecallCoLoi_KhongTinhSach()
    {
        var stats = NewStats();

        stats.Apply(Recall(mistakes: 1, hints: 0), Day1);

        stats.CleanRecallDays.Should().Be(0);
    }

    [Fact]
    public void GuidedSach_KhongTinhSach_DuKhongLoiKhongGoiY()
    {
        var stats = NewStats();

        // Bước "Tô theo" (guided) KHÔNG BAO GIỜ sạch dù 0 lỗi/0 gợi ý — chỉ "Tự viết" (recall) chứng
        // minh đã thuộc (R-W2/R-W5).
        stats.Apply(Guided(mistakes: 0, hints: 0), Day1);
        stats.Apply(Guided(mistakes: 0, hints: 0), Day2);

        stats.CleanRecallDays.Should().Be(0);
        stats.MasteryStatus.Should().Be(MasteryStatuses.Practicing);
        stats.GuidedAttempts.Should().Be(2);
        stats.RecallAttempts.Should().Be(0);
    }

    [Theory]
    [InlineData(2, 0, true)]
    [InlineData(0, 1, true)]
    [InlineData(1, 0, false)]
    [InlineData(0, 0, false)]
    public void IsWeak_KhiLoiHoacGoiYGanNhatVuotNguong(int lastMistakes, int lastHints, bool expected)
    {
        var stats = NewStats();
        stats.Apply(Recall(mistakes: lastMistakes, hints: lastHints), Day1);

        stats.IsWeak.Should().Be(expected);
    }

    [Fact]
    public void DaMastered_KhongConIsWeak_DuLanGanNhatTe()
    {
        var stats = NewStats();
        stats.Apply(Recall(), Day1);
        stats.Apply(Recall(), Day2); // mastered

        stats.Apply(Guided(mistakes: 5, hints: 3), Day2); // lần gần nhất tệ, nhưng đã thuộc

        stats.MasteryStatus.Should().Be(MasteryStatuses.Mastered);
        stats.IsWeak.Should().BeFalse();
    }

    [Fact]
    public void BestRecallMistakes_LaMin_ChuaRecallLaNull()
    {
        var stats = NewStats();
        stats.BestRecallMistakes.Should().BeNull();

        stats.Apply(Recall(mistakes: 3), Day1);
        stats.BestRecallMistakes.Should().Be(3);

        stats.Apply(Recall(mistakes: 1), Day2);
        stats.BestRecallMistakes.Should().Be(1);

        stats.Apply(Recall(mistakes: 5), Day2);
        stats.BestRecallMistakes.Should().Be(1); // không tăng lại
    }

    [Fact]
    public void Guided_KhongAnhHuongBestRecallMistakes()
    {
        var stats = NewStats();

        stats.Apply(Guided(mistakes: 0), Day1);

        stats.BestRecallMistakes.Should().BeNull();
    }
}
