using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Domain.Lessons;
using AntFarm.Chinese.Infrastructure.Persistence;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Lessons;

/// <summary>
/// §5.2.1.6 test 0 — sau khởi động (importer chạy với <c>content/chinese</c> THẬT, cùng kỹ thuật
/// <c>RealContentTests</c>) có đúng 5 bài <c>source='seed'</c> theo §5.4.6; khởi động thêm 2 lần
/// không nhân đôi dữ liệu.
/// </summary>
[Collection(ChineseApiCollection.Name)]
public class RealLessonsTests : IClassFixture<ChineseDbApiFactory>
{
    private static readonly string[] ExpectedSlugs = ["chao-hoi", "ban-than", "so-dem", "gia-dinh", "thoi-gian"];

    public RealLessonsTests(ChineseDbApiFactory factory)
    {
        Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());
        using var _ = factory.CreateClient(); // buộc host khởi động (importer chạy) trước khi test đọc DB
    }

    [DbFact]
    public async Task NamBaiSeedThat_CoDuSlug_TatCaPublished()
    {
        await using var db = TestDbContextFactory.Create();

        var lessons = await db.Lessons.AsNoTracking().Where(l => l.Source == LessonSources.Seed).ToListAsync();

        lessons.Should().HaveCount(5);
        lessons.Select(l => l.Slug).Should().BeEquivalentTo(ExpectedSlugs);
        lessons.Should().AllSatisfy(l =>
        {
            l.Status.Should().Be(LessonStatuses.Published);
            l.ReviewStatus.Should().Be(LessonReviewStatuses.Machine);
            l.SourceHash.Should().NotBeNullOrWhiteSpace();
        });
    }

    [DbFact]
    public async Task KhoiDongLaiHaiLanNua_KhongNhanDoiDuLieu()
    {
        await using var dbBefore = TestDbContextFactory.Create();
        var lessonsBefore = await dbBefore.Lessons.CountAsync();
        var blocksBefore = await dbBefore.LessonBlocks.CountAsync();
        var wordsBefore = await dbBefore.LessonWords.CountAsync();
        var questionsBefore = await dbBefore.QuizQuestions.CountAsync();
        var succeededRunsBefore = await dbBefore.ImportRuns.CountAsync(r => r.Dataset == "lessons" && r.Status == "succeeded");

        for (var i = 0; i < 2; i++)
        {
            using var factory = new ChineseDbApiFactory();
            Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());
            using var client = factory.CreateClient();
            (await client.GetAsync("/health/live")).EnsureSuccessStatusCode();
        }

        await using var dbAfter = TestDbContextFactory.Create();
        (await dbAfter.Lessons.CountAsync()).Should().Be(lessonsBefore);
        (await dbAfter.LessonBlocks.CountAsync()).Should().Be(blocksBefore);
        (await dbAfter.LessonWords.CountAsync()).Should().Be(wordsBefore);
        (await dbAfter.QuizQuestions.CountAsync()).Should().Be(questionsBefore);
        (await dbAfter.ImportRuns.CountAsync(r => r.Dataset == "lessons" && r.Status == "succeeded")).Should().Be(succeededRunsBefore);
    }

    private static string ResolveRealContentRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "content", "chinese");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return "content/chinese";
    }
}
