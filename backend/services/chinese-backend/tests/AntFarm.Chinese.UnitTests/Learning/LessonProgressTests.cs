using AntFarm.Chinese.Domain.Learning;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Learning;

/// <summary>§5.2.1.6 — R-LS5: lần đạt đầu trả <c>true</c>, lần đạt sau <c>false</c>, <c>best</c> là max, trượt sau khi đã đạt không hạ <c>status</c>.</summary>
public class LessonProgressTests
{
    private static readonly DateTime Start = new(2026, 9, 17, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void LanDatDauTien_TraTrue_ChuyenCompleted()
    {
        var progress = LessonProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Start, Start);

        var firstCompletion = progress.ApplyAttempt(85, true, Start.AddMinutes(5));

        firstCompletion.Should().BeTrue();
        progress.Status.Should().Be(LessonProgressStatuses.Completed);
        progress.CompletedAt.Should().Be(Start.AddMinutes(5));
        progress.BestScorePercent.Should().Be(85);
        progress.AttemptsCount.Should().Be(1);
    }

    [Fact]
    public void LanDatSau_TraFalse_KhongDoiCompletedAt()
    {
        var progress = LessonProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Start, Start);
        progress.ApplyAttempt(85, true, Start.AddMinutes(5));

        var secondCompletion = progress.ApplyAttempt(90, true, Start.AddMinutes(10));

        secondCompletion.Should().BeFalse();
        progress.Status.Should().Be(LessonProgressStatuses.Completed);
        progress.CompletedAt.Should().Be(Start.AddMinutes(5)); // KHÔNG đổi — lần đạt đầu tiên mới quyết định
        progress.BestScorePercent.Should().Be(90); // best vẫn cập nhật = max
        progress.AttemptsCount.Should().Be(2);
    }

    [Fact]
    public void BestScorePercent_LaMax_KhongHaKhiLanSauThapHon()
    {
        var progress = LessonProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Start, Start);
        progress.ApplyAttempt(90, true, Start.AddMinutes(5));

        progress.ApplyAttempt(60, false, Start.AddMinutes(10));

        progress.BestScorePercent.Should().Be(90);
    }

    [Fact]
    public void TruotSauKhiDaDat_KhongHaStatus()
    {
        var progress = LessonProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Start, Start);
        progress.ApplyAttempt(90, true, Start.AddMinutes(5));

        var firstCompletion = progress.ApplyAttempt(40, false, Start.AddMinutes(10));

        firstCompletion.Should().BeFalse();
        progress.Status.Should().Be(LessonProgressStatuses.Completed); // vẫn completed, không lùi về in_progress
    }

    [Fact]
    public void ChuaDatLanNao_VanInProgress()
    {
        var progress = LessonProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Start, Start);

        var firstCompletion = progress.ApplyAttempt(50, false, Start.AddMinutes(5));

        firstCompletion.Should().BeFalse();
        progress.Status.Should().Be(LessonProgressStatuses.InProgress);
        progress.CompletedAt.Should().BeNull();
    }
}
