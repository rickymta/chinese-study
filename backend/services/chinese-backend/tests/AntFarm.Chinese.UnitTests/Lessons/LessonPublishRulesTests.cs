using AntFarm.Chinese.Application.Lessons;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Lessons;

/// <summary>§5.2.1.6 — R-CA4: điều kiện xuất bản (problems chặn) vs cảnh báo (warnings không chặn).</summary>
public class LessonPublishRulesTests
{
    private static PublishCheckQuestion Q(string type = "single_choice", int options = 3, bool correctInOptions = true, string? audioText = null) =>
        new(type, options, correctInOptions, audioText);

    [Fact]
    public void ThieuQuiz_TaoProblem()
    {
        var input = new PublishCheckInput("Bài học", 2, true, 10, [Q(), Q()]); // chỉ 2 câu, cần ≥ 3

        var (problems, _) = LessonPublishRules.Check(input);

        problems.Should().Contain(p => p.Contains("quiz"));
    }

    [Fact]
    public void BaCauDeuSingleChoice_ChiCanhBaoKhongChan()
    {
        var input = new PublishCheckInput("Bài học", 3, true, 10, [Q(), Q(), Q()]);

        var (problems, warnings) = LessonPublishRules.Check(input);

        problems.Should().BeEmpty();
        warnings.Should().Contain(w => w.Contains("nghe"));
    }

    [Fact]
    public void TieuDeRong_TaoProblem()
    {
        var input = new PublishCheckInput("", 3, true, 10, [Q(), Q(), Q()]);

        var (problems, _) = LessonPublishRules.Check(input);

        problems.Should().Contain(p => p.Contains("Tiêu đề"));
    }

    [Fact]
    public void KhongCoTu_TaoProblem()
    {
        var input = new PublishCheckInput("Bài học", 3, true, 0, [Q(), Q(), Q()]);

        var (problems, _) = LessonPublishRules.Check(input);

        problems.Should().Contain(p => p.Contains("từ"));
    }

    [Fact]
    public void KhongCoKhoi_TaoProblem()
    {
        var input = new PublishCheckInput("Bài học", 0, false, 10, [Q(), Q(), Q()]);

        var (problems, _) = LessonPublishRules.Check(input);

        problems.Should().Contain(p => p.Contains("khối"));
    }

    [Fact]
    public void ListenChoiceThieuAudioText_TaoProblem()
    {
        var input = new PublishCheckInput("Bài học", 3, true, 10, [Q("listen_choice", audioText: null), Q(), Q()]);

        var (problems, _) = LessonPublishRules.Check(input);

        problems.Should().Contain(p => p.Contains("nghe"));
    }

    [Fact]
    public void CorrectOptionKhongThuocLuaChon_TaoProblem()
    {
        var input = new PublishCheckInput("Bài học", 3, true, 10, [Q(correctInOptions: false), Q(), Q()]);

        var (problems, _) = LessonPublishRules.Check(input);

        problems.Should().Contain(p => p.Contains("correctOptionId"));
    }

    [Fact]
    public void DuDieuKienVaKhongCoCanhBao_HopLe()
    {
        var questions = new List<PublishCheckQuestion>
        {
            Q("listen_choice", audioText: "你好"),
            Q("listen_choice", audioText: "谢谢"),
            Q(),
            Q(),
            Q()
        };
        var input = new PublishCheckInput("Bài học", 5, true, 10, questions);

        var (problems, warnings) = LessonPublishRules.Check(input);

        problems.Should().BeEmpty();
        warnings.Should().BeEmpty();
    }

    [Fact]
    public void NhieuTuHonKhuyenNghi_ChiCanhBao()
    {
        var questions = new List<PublishCheckQuestion> { Q("listen_choice", audioText: "你好"), Q(), Q(), Q(), Q() };
        var input = new PublishCheckInput("Bài học", 5, true, 20, questions); // > 15 từ

        var (problems, warnings) = LessonPublishRules.Check(input);

        problems.Should().BeEmpty();
        warnings.Should().Contain(w => w.Contains("từ"));
    }

    [Fact]
    public void KhongCoKhoiHoiThoai_ChiCanhBao()
    {
        var questions = new List<PublishCheckQuestion> { Q("listen_choice", audioText: "你好"), Q(), Q(), Q(), Q() };
        var input = new PublishCheckInput("Bài học", 3, false, 10, questions);

        var (problems, warnings) = LessonPublishRules.Check(input);

        problems.Should().BeEmpty();
        warnings.Should().Contain(w => w.Contains("dialogue"));
    }
}
