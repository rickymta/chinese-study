using System.Text.RegularExpressions;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Chinese.Infrastructure.Content;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Lessons;

/// <summary>
/// §5.2.1.6 test 1–2 — <c>LessonImporter</c> TRỰC TIẾP (không qua HTTP) trên thư mục TẠM (copy từ
/// <c>TestData/content/lessons</c>, ĐỔI TÊN slug theo hậu tố riêng của mỗi lần gọi — <c>content.lessons.slug</c>
/// UNIQUE toàn cục và KHÔNG scope theo dataset như <c>import_runs</c>, hai test method dùng CHUNG
/// fixture gốc sẽ đụng nhau nếu giữ nguyên slug "test-bai"/"test-nhap"/"test-loi").
/// </summary>
[Collection(ChineseApiCollection.Name)]
public partial class LessonImporterTests
{
    private static string FixturesDir => Path.Combine(AppContext.BaseDirectory, "TestData", "content", "lessons");

    [GeneratedRegex(@"^(\d{2})-([a-z0-9-]+)\.json$")]
    private static partial Regex FileNamePattern();

    /// <summary>Copy fixture ra thư mục tạm, đổi <c>slug</c> (cả trong tên file lẫn nội dung JSON) thêm hậu tố riêng để không đụng lần gọi khác trong CÙNG DB test dùng chung.</summary>
    private static (string Dir, string TestBaiSlug, string TestNhapSlug, string TestLoiSlug) CreateTempDirFromFixtures(string suffix)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"af-lessons-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        string? testBaiSlug = null, testNhapSlug = null, testLoiSlug = null;

        foreach (var file in Directory.GetFiles(FixturesDir, "*.json"))
        {
            var fileName = Path.GetFileName(file);
            var match = FileNamePattern().Match(fileName);
            var order = match.Groups[1].Value;
            var baseSlug = match.Groups[2].Value;
            var newSlug = $"{baseSlug}-{suffix}";

            switch (baseSlug)
            {
                case "test-bai": testBaiSlug = newSlug; break;
                case "test-nhap": testNhapSlug = newSlug; break;
                case "test-loi": testLoiSlug = newSlug; break;
            }

            var json = File.ReadAllText(file).Replace($"\"slug\": \"{baseSlug}\"", $"\"slug\": \"{newSlug}\"", StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(dir, $"{order}-{newSlug}.json"), json);
        }

        return (dir, testBaiSlug!, testNhapSlug!, testLoiSlug!);
    }

    [DbFact]
    public async Task NapThuMucTest_TaoBaiHopLe_BoQuaBaiLoi_NapLaiKhongNhanDoi()
    {
        var (dir, testBaiSlug, _, testLoiSlug) = CreateTempDirFromFixtures($"a{Guid.NewGuid():N}"[..12]);
        var dataset = $"lessons-test-{Guid.NewGuid():N}";
        try
        {
            await using var db = TestDbContextFactory.Create();
            var importer = new LessonImporter(db, TimeProvider.System, NullLogger<LessonImporter>.Instance);

            var first = await importer.ImportDirectoryAsync(dir, dataset, CancellationToken.None);
            first.Status.Should().Be(ImportRunStatus.Succeeded);
            first.Inserted.Should().Be(2); // test-bai (published) + test-nhap (draft) — test-loi bị bỏ qua (R-LS15)
            first.Invalid.Should().Be(1);

            var testBai = await db.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.Slug == testBaiSlug);
            testBai.Should().NotBeNull();
            (await db.Lessons.AsNoTracking().AnyAsync(l => l.Slug == testLoiSlug)).Should().BeFalse();

            var run = await db.ImportRuns.AsNoTracking()
                .Where(r => r.Dataset == dataset)
                .OrderByDescending(r => r.StartedAt)
                .FirstAsync();
            run.Invalid.Should().Be(1);

            var lessonsBefore = await db.Lessons.CountAsync();
            var blocksBefore = await db.LessonBlocks.CountAsync();
            var questionsBefore = await db.QuizQuestions.CountAsync();
            var questionIdsBefore = await db.QuizQuestions.Where(q => q.LessonId == testBai!.Id).Select(q => q.Id).ToListAsync();

            // Nạp lại THƯ MỤC KHÔNG ĐỔI — hash tổng hợp khớp lần succeeded gần nhất ⇒ Skipped, id câu hỏi không đổi.
            var second = await importer.ImportDirectoryAsync(dir, dataset, CancellationToken.None);
            second.Status.Should().Be(ImportRunStatus.Skipped);

            (await db.Lessons.CountAsync()).Should().Be(lessonsBefore);
            (await db.LessonBlocks.CountAsync()).Should().Be(blocksBefore);
            (await db.QuizQuestions.CountAsync()).Should().Be(questionsBefore);
            var questionIdsAfter = await db.QuizQuestions.Where(q => q.LessonId == testBai!.Id).Select(q => q.Id).ToListAsync();
            questionIdsAfter.Should().BeEquivalentTo(questionIdsBefore);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [DbFact]
    public async Task SuaFileTest_CapNhatTaiCho_BaiDaSuaTayKhongBiGhiDe()
    {
        var (dir, testBaiSlug, _, _) = CreateTempDirFromFixtures($"b{Guid.NewGuid():N}"[..12]);
        var dataset = $"lessons-test-{Guid.NewGuid():N}";
        try
        {
            await using var db = TestDbContextFactory.Create();
            var importer = new LessonImporter(db, TimeProvider.System, NullLogger<LessonImporter>.Instance);
            await importer.ImportDirectoryAsync(dir, dataset, CancellationToken.None);

            // Bước 2 (§5.2.1.6): sửa file test-bai — bài CHƯA sửa tay phải được cập nhật tại chỗ.
            var testBaiPath = Path.Combine(dir, $"01-{testBaiSlug}.json");
            var originalJson = await File.ReadAllTextAsync(testBaiPath);
            var editedJson = originalJson.Replace("Bài kiểm thử", "Bài kiểm thử (đã sửa)", StringComparison.Ordinal);
            await File.WriteAllTextAsync(testBaiPath, editedJson);

            var second = await importer.ImportDirectoryAsync(dir, dataset, CancellationToken.None);
            second.Status.Should().Be(ImportRunStatus.Succeeded);
            second.Updated.Should().Be(1);

            var updated = await db.Lessons.AsNoTracking().FirstAsync(l => l.Slug == testBaiSlug);
            updated.Title.Should().Be("Bài kiểm thử (đã sửa)");

            // Đặt edited_at (mô phỏng admin F10 đã sửa tay) rồi nạp lại — KHÔNG được ghi đè (R-LS14).
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE content.lessons SET edited_at = now() WHERE slug = {testBaiSlug}");
            db.ChangeTracker.Clear();

            var editedJsonAgain = editedJson.Replace("Bài kiểm thử (đã sửa)", "Bài kiểm thử (SỬA LẦN 2 — KHÔNG ĐƯỢC NẠP)", StringComparison.Ordinal);
            await File.WriteAllTextAsync(testBaiPath, editedJsonAgain);

            var third = await importer.ImportDirectoryAsync(dir, dataset, CancellationToken.None);
            third.Status.Should().Be(ImportRunStatus.Succeeded);
            third.Protected.Should().Be(1);

            var untouched = await db.Lessons.AsNoTracking().FirstAsync(l => l.Slug == testBaiSlug);
            untouched.Title.Should().Be("Bài kiểm thử (đã sửa)"); // KHÔNG đổi sang "SỬA LẦN 2"
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
