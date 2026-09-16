using AntFarm.Chinese.Application.Dictionary;
using AntFarm.Chinese.Application.Pinyin;
using AntFarm.Chinese.Infrastructure.Content;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Dictionary;

/// <summary>§5.2.5 — dùng học liệu pinyin THẬT của repo (giống PinyinCatalogLoaderTests) để tách âm tiết đúng.</summary>
public class DictionaryQueryParserTests
{
    private readonly DictionaryQueryParser _parser = new(LoadRealCatalog());

    [Fact]
    public void Parse_ChuHan_RaHanzi()
    {
        var parsed = _parser.Parse("爱");
        parsed.Kind.Should().Be(DictionaryQueryKind.Hanzi);
        parsed.HanziText.Should().Be("爱");
    }

    [Fact]
    public void Parse_Ai_CoCaPinyinCompactVaViHasDiacritics()
    {
        var parsed = _parser.Parse("ái");
        parsed.PinyinCompact.Should().Be("ai2");
        parsed.ViText.Should().Be("ái");
        parsed.ViHasDiacritics.Should().BeTrue();
    }

    [Fact]
    public void Parse_Yeu_KhongDauChoPinyinTonelessVaViPlain()
    {
        var parsed = _parser.Parse("yeu");
        parsed.PinyinCompact.Should().BeNull();
        parsed.PinyinToneless.Should().Be("yeu"); // hợp lệ về KÝ TỰ — DB không có mục nào pinyin_search='yeu' nên vô hại
        parsed.ViPlain.Should().Be("yeu");
        parsed.ViHasDiacritics.Should().BeFalse();
    }

    [Fact]
    public void Parse_YeuCoDau_KhongCoKhoaPinyin()
    {
        var parsed = _parser.Parse("yêu");
        parsed.PinyinCompact.Should().BeNull();
        parsed.PinyinToneless.Should().BeNull();
        parsed.ViHasDiacritics.Should().BeTrue();
    }

    [Fact]
    public void Parse_NiHaoLien_TachDungPinyinCompact()
    {
        _parser.Parse("nǐhǎo").PinyinCompact.Should().Be("ni3hao3");
        _parser.Parse("ni3hao3").PinyinCompact.Should().Be("ni3hao3");
        _parser.Parse("ni hao").PinyinToneless.Should().Be("nihao");
    }

    [Fact]
    public void Parse_Rong_RaEmpty() => _parser.Parse("   ").Kind.Should().Be(DictionaryQueryKind.Empty);

    private static IPinyinCatalog LoadRealCatalog()
    {
        var repoRoot = FindRepoRoot() ?? throw new InvalidOperationException("Không tìm thấy content/chinese từ AppContext.BaseDirectory.");
        return PinyinCatalogLoader.Load(Path.Combine(repoRoot, "content", "chinese"), NullLogger.Instance);
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
}
