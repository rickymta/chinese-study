using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Infrastructure.Content;
using AntFarm.Chinese.Infrastructure.Persistence;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Content;

/// <summary>
/// §5.2.5 — <c>ContentImporter</c> trực tiếp (không qua HTTP), dùng tệp giả <c>TestData/content/</c>
/// (không trùng dữ liệu HSK1 thật). Mỗi test dùng CHỮ/TỪ RIÊNG (không chia sẻ) — bảng
/// <c>content.words</c>/<c>content.characters</c> KHÔNG scope theo dataset (chỉ <c>import_runs</c>
/// mới scope), test song song cùng khoá tự nhiên sẽ ảnh hưởng lẫn nhau.
/// </summary>
[Collection(ChineseApiCollection.Name)]
public class ContentImporterTests
{
    private static string TestDataPath(string fileName) => Path.Combine(AppContext.BaseDirectory, "TestData", "content", fileName);

    [DbFact]
    public async Task NapHaiLan_LanHaiBoQuaKhongNhanDoi()
    {
        var dataset = $"characters-basic-{Guid.NewGuid():N}";
        var wordDataset = $"hsk-words-basic-{Guid.NewGuid():N}";

        await using var db = TestDbContextFactory.Create();
        var importer = new ContentImporter(db, TimeProvider.System, NullLogger<ContentImporter>.Instance);

        var firstChars = await importer.ImportCharactersAsync(TestDataPath("characters-basic.json"), dataset, CancellationToken.None);
        firstChars.Status.Should().Be(ImportRunStatus.Succeeded);
        firstChars.Inserted.Should().Be(2);

        var firstWords = await importer.ImportWordsAsync(TestDataPath("hsk-words-basic.json"), wordDataset, CancellationToken.None);
        firstWords.Status.Should().Be(ImportRunStatus.Succeeded);
        firstWords.Inserted.Should().Be(1);

        var charsCountAfterFirst = await db.Characters.CountAsync(c => c.Hanzi == "测" || c.Hanzi == "验");
        var wordsCountAfterFirst = await db.Words.CountAsync(w => w.Simplified == "测验");
        charsCountAfterFirst.Should().Be(2);
        wordsCountAfterFirst.Should().Be(1);

        var secondChars = await importer.ImportCharactersAsync(TestDataPath("characters-basic.json"), dataset, CancellationToken.None);
        secondChars.Status.Should().Be(ImportRunStatus.Skipped);

        var secondWords = await importer.ImportWordsAsync(TestDataPath("hsk-words-basic.json"), wordDataset, CancellationToken.None);
        secondWords.Status.Should().Be(ImportRunStatus.Skipped);

        (await db.Characters.CountAsync(c => c.Hanzi == "测" || c.Hanzi == "验")).Should().Be(2);
        (await db.Words.CountAsync(w => w.Simplified == "测验")).Should().Be(1);
    }

    [DbFact]
    public async Task DoiVersionDaGhiNhan_ChayLaiVaTatCaUnchanged()
    {
        var charsDataset = $"characters-version-{Guid.NewGuid():N}";
        var wordsDataset = $"hsk-words-version-{Guid.NewGuid():N}";

        await using var db = TestDbContextFactory.Create();
        var importer = new ContentImporter(db, TimeProvider.System, NullLogger<ContentImporter>.Instance);

        await importer.ImportCharactersAsync(TestDataPath("characters-version.json"), charsDataset, CancellationToken.None);
        var firstWords = await importer.ImportWordsAsync(TestDataPath("hsk-words-version.json"), wordsDataset, CancellationToken.None);
        firstWords.Status.Should().Be(ImportRunStatus.Succeeded);
        firstWords.Inserted.Should().Be(1);

        // Giả lập "logic nạp đã đổi" (Version cũ khác Version hiện tại) BẰNG CÁCH sửa thẳng bản ghi
        // import_runs đã ghi — không cần đổi ContentImporter.Version (hằng số biên dịch).
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE content.import_runs SET importer_version = -1 WHERE dataset = {wordsDataset}");
        db.ChangeTracker.Clear();

        var secondWords = await importer.ImportWordsAsync(TestDataPath("hsk-words-version.json"), wordsDataset, CancellationToken.None);

        secondWords.Status.Should().Be(ImportRunStatus.Succeeded);
        secondWords.Inserted.Should().Be(0);
        secondWords.Updated.Should().Be(0);
        secondWords.Unchanged.Should().Be(1); // = tổng số dòng trong tệp (1)
    }

    [DbFact]
    public async Task DongDaDuyet_KhongBiGhiDeVaProtectedTang()
    {
        var charsDataset = $"characters-protected-{Guid.NewGuid():N}";
        var wordsDataset = $"hsk-words-protected-{Guid.NewGuid():N}";

        await using var db = TestDbContextFactory.Create();
        var importer = new ContentImporter(db, TimeProvider.System, NullLogger<ContentImporter>.Instance);

        await importer.ImportCharactersAsync(TestDataPath("characters-protected.json"), charsDataset, CancellationToken.None);
        await importer.ImportWordsAsync(TestDataPath("hsk-words-protected-v1.json"), wordsDataset, CancellationToken.None);

        // Đánh dấu đã duyệt (F10) trực tiếp trên DB — ContentImporter không có API sửa, đúng ý đồ.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE content.words SET meaning_vi_status = 'reviewed' WHERE simplified = '淼鑫'");
        db.ChangeTracker.Clear();

        var result = await importer.ImportWordsAsync(TestDataPath("hsk-words-protected-v2.json"), wordsDataset, CancellationToken.None);

        result.Status.Should().Be(ImportRunStatus.Succeeded);
        result.Protected.Should().Be(1);

        var word = await db.Words.AsNoTracking().FirstAsync(w => w.Simplified == "淼鑫");
        word.MeaningsVi.Should().Equal("nghĩa ban đầu"); // KHÔNG bị ghi đè bởi v2
        word.MeaningViStatus.Should().Be(MeaningViStatus.Reviewed);
    }

    [DbFact]
    public async Task TepJsonHong_GhiImportRunsFailedKhongNem()
    {
        var dataset = $"hsk-words-invalid-{Guid.NewGuid():N}";
        var tempPath = Path.Combine(Path.GetTempPath(), $"af-content-invalid-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(tempPath, "{ đây không phải JSON hợp lệ");

        try
        {
            await using var db = TestDbContextFactory.Create();
            var importer = new ContentImporter(db, TimeProvider.System, NullLogger<ContentImporter>.Instance);

            var act = () => importer.ImportWordsAsync(tempPath, dataset, CancellationToken.None);
            var result = await act.Should().NotThrowAsync();
            result.Subject.Status.Should().Be(ImportRunStatus.Failed);
            result.Subject.Error.Should().NotBeNullOrWhiteSpace();

            var run = await db.ImportRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Dataset == dataset);
            run.Should().NotBeNull();
            run!.Status.Should().Be("failed");
            run.Error.Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [DbFact]
    public async Task TepKhongTonTai_BoQuaKhongGhiImportRuns()
    {
        var dataset = $"hsk-words-missing-{Guid.NewGuid():N}";
        await using var db = TestDbContextFactory.Create();
        var importer = new ContentImporter(db, TimeProvider.System, NullLogger<ContentImporter>.Instance);

        var result = await importer.ImportWordsAsync(Path.Combine(AppContext.BaseDirectory, "TestData", "content", "khong-ton-tai.json"), dataset, CancellationToken.None);

        result.Status.Should().Be(ImportRunStatus.Skipped);
        (await db.ImportRuns.AnyAsync(r => r.Dataset == dataset)).Should().BeFalse();
    }
}
