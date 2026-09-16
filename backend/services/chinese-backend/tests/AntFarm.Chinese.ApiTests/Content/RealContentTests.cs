using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Chinese.Infrastructure.Persistence;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Content;

/// <summary>
/// §5.2.5 — sau khi <see cref="ChineseDbApiFactory"/> khởi động (importer chạy với
/// <c>content/chinese</c> THẬT trong khối <c>ContentImportRunner.RunAsync</c> của <c>Program.cs</c>,
/// ĐỘC LẬP với <c>AutoMigrate</c> — xem ghi chú ở đó): kiểm 500 từ HSK1, <c>path_order</c> liên tục,
/// và khởi động lại KHÔNG nhân đôi dữ liệu.
/// </summary>
[Collection(ChineseApiCollection.Name)]
public class RealContentTests : IClassFixture<ChineseDbApiFactory>
{
    public RealContentTests(ChineseDbApiFactory factory)
    {
        // Đảm bảo importer đọc ĐÚNG content/chinese thật của repo (không phụ thuộc thứ tự chạy so
        // với PinyinApiTests — cùng kỹ thuật, RK37).
        Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());
        using var _ = factory.CreateClient(); // buộc host khởi động (importer chạy) trước khi test đọc DB
    }

    [DbFact]
    public async Task Hsk1_Co500Tu_PathOrderLienTuc1Den500()
    {
        await using var db = TestDbContextFactory.Create();

        var hsk1Count = await db.Words.CountAsync(w => w.Hsk3Level == 1);
        hsk1Count.Should().Be(500);

        var pathOrders = await db.Words
            .Where(w => w.Hsk3Level == 1)
            .Select(w => w.PathOrder)
            .OrderBy(p => p)
            .ToListAsync();

        pathOrders.Should().AllSatisfy(p => p.Should().NotBeNull());
        pathOrders.Select(p => p!.Value).Should().Equal(Enumerable.Range(1, 500));
    }

    [DbFact]
    public async Task KhoiDongLaiHaiLanNua_KhongNhanDoiDuLieu()
    {
        await using var dbBefore = TestDbContextFactory.Create();
        var wordsBefore = await dbBefore.Words.CountAsync();
        var charactersBefore = await dbBefore.Characters.CountAsync();
        var linksBefore = await dbBefore.WordCharacters.CountAsync();
        var succeededRunsBefore = await dbBefore.ImportRuns.CountAsync(r => r.Status == "succeeded");

        for (var i = 0; i < 2; i++)
        {
            using var factory = new ChineseDbApiFactory();
            Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());
            using var client = factory.CreateClient(); // khởi động host mới ⇒ ContentImportRunner chạy lại
            (await client.GetAsync("/health/live")).EnsureSuccessStatusCode();
        }

        await using var dbAfter = TestDbContextFactory.Create();
        (await dbAfter.Words.CountAsync()).Should().Be(wordsBefore);
        (await dbAfter.Characters.CountAsync()).Should().Be(charactersBefore);
        (await dbAfter.WordCharacters.CountAsync()).Should().Be(linksBefore);
        // Hash tệp không đổi ⇒ Skipped ⇒ KHÔNG thêm dòng import_runs succeeded mới.
        (await dbAfter.ImportRuns.CountAsync(r => r.Status == "succeeded")).Should().Be(succeededRunsBefore);
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
