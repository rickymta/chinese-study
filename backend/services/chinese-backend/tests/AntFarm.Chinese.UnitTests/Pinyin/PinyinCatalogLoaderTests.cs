using AntFarm.Chinese.Infrastructure.Content;
using AntFarm.Core.Errors;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Pinyin;

public class PinyinCatalogLoaderTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "af-pinyin-catalog-tests-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Fact]
    public void Load_BoFileHopLeToiThieu_KhaDung()
    {
        WriteMinimalValidFiles(_tempRoot);

        var catalog = PinyinCatalogLoader.Load(_tempRoot, NullLogger.Instance);

        catalog.IsAvailable.Should().BeTrue();
        catalog.Version.Should().NotBeNullOrEmpty();
        catalog.ContainsSyllable("ma").Should().BeTrue();
    }

    [Fact]
    public void Load_ThieuGuideJson_KhongKhaDungKhongNem()
    {
        WriteMinimalValidFiles(_tempRoot);
        File.Delete(Path.Combine(_tempRoot, "data", "pinyin", "guide.json"));

        var act = () => PinyinCatalogLoader.Load(_tempRoot, NullLogger.Instance);

        act.Should().NotThrow();
        act().IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Load_JsonHong_KhongKhaDungKhongNem()
    {
        WriteMinimalValidFiles(_tempRoot);
        File.WriteAllText(Path.Combine(_tempRoot, "data", "pinyin", "syllables.json"), "{ không phải json hợp lệ");

        var act = () => PinyinCatalogLoader.Load(_tempRoot, NullLogger.Instance);

        act.Should().NotThrow();
        act().IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Load_KhoaAmTietTrung_KhongKhaDung()
    {
        WriteMinimalValidFiles(_tempRoot);
        var syllablesPath = Path.Combine(_tempRoot, "data", "pinyin", "syllables.json");
        File.WriteAllText(syllablesPath, """
            { "dataset": "pinyin-syllables", "version": "2026-09-17", "items": [
              { "syllable": "ma", "initial": "m", "final": "a", "tones": {} },
              { "syllable": "ma", "initial": "m", "final": "a", "tones": {} }
            ] }
            """);

        var catalog = PinyinCatalogLoader.Load(_tempRoot, NullLogger.Instance);

        catalog.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Chart_KhiKhongKhaDung_NemServiceUnavailable()
    {
        var catalog = PinyinCatalogLoader.Load(_tempRoot, NullLogger.Instance); // thư mục không tồn tại

        catalog.IsAvailable.Should().BeFalse();
        var act = () => catalog.Chart;
        act.Should().Throw<ServiceUnavailableException>().Which.Code.Should().Be("CONTENT_UNAVAILABLE");
    }

    [Fact]
    public void Load_HocLieuThatCuaRepo_KhaDungVaDuDoPhu()
    {
        var repoRoot = FindRepoRoot();
        if (repoRoot is null)
        {
            // RK37: môi trường build/CI có thể không thấy được content/chinese theo đường dẫn
            // tương đối — không fail âm thầm, log rõ để integration biết bật lại.
            Assert.Fail("Không tìm thấy content/chinese từ AppContext.BaseDirectory — kiểm cấu trúc thư mục khi chạy trên CI/Docker.");
            return;
        }

        var contentRoot = Path.Combine(repoRoot, "content", "chinese");
        var catalog = PinyinCatalogLoader.Load(contentRoot, NullLogger.Instance);

        catalog.IsAvailable.Should().BeTrue();
        catalog.Chart.Syllables.Should().HaveCountGreaterOrEqualTo(380);

        var toneExampleCount = catalog.Chart.Syllables.Sum(s => s.Tones.Count);
        toneExampleCount.Should().BeGreaterOrEqualTo(300);
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "content", "chinese")))
                return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }

    private static void WriteMinimalValidFiles(string root)
    {
        var dataDir = Path.Combine(root, "data", "pinyin");
        Directory.CreateDirectory(dataDir);

        File.WriteAllText(Path.Combine(dataDir, "initials.json"), """
            { "dataset": "pinyin-initials", "version": "2026-09-17", "items": [
              { "code": "", "group": "khong", "display": "", "ipa": "", "aspirated": false, "noteVi": "x", "examples": [] },
              { "code": "m", "group": "moi", "display": "m", "ipa": "m", "aspirated": false, "noteVi": "x", "examples": [] }
            ] }
            """);

        File.WriteAllText(Path.Combine(dataDir, "finals.json"), """
            { "dataset": "pinyin-finals", "version": "2026-09-17", "items": [
              { "code": "a", "group": "don", "display": "a", "standaloneSpelling": "a", "noteVi": "x" }
            ] }
            """);

        File.WriteAllText(Path.Combine(dataDir, "syllables.json"), """
            { "dataset": "pinyin-syllables", "version": "2026-09-17", "items": [
              { "syllable": "ma", "initial": "m", "final": "a", "tones": {
                "1": { "hanzi": "妈", "meaningVi": "mẹ" },
                "3": { "hanzi": "马", "meaningVi": "ngựa" }
              } }
            ] }
            """);

        File.WriteAllText(Path.Combine(dataDir, "guide.json"), """
            { "dataset": "pinyin-guide", "version": "2026-09-17", "items": [
              { "id": "bon-thanh", "title": "Bốn thanh", "order": 1, "blocks": [
                { "type": "paragraph", "text": "x" }
              ] }
            ] }
            """);
    }
}
