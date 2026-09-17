using System.Text.Json;
using AntFarm.Chinese.Domain.Pinyin;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Pinyin;

/// <summary>
/// Đối chiếu <see cref="PinyinSyllableTable.Keys"/> (nhúng cứng, F6) với
/// <c>content/chinese/data/pinyin/syllables.json</c> (F5, nguồn số liệu gốc) — hai nơi PHẢI khớp
/// TUYỆT ĐỐI, lệch nghĩa là bảng nhúng cứng đã lỗi thời so với học liệu thật (review F6.2).
/// </summary>
public class PinyinSyllableTableTests
{
    [Fact]
    public void Keys_KhopTuyetDoiVoiSyllablesJsonThat()
    {
        var repoRoot = FindRepoRoot();
        if (repoRoot is null)
        {
            Assert.Fail("Không tìm thấy content/chinese từ AppContext.BaseDirectory — kiểm cấu trúc thư mục khi chạy trên CI/Docker (RK37).");
            return;
        }

        var path = Path.Combine(repoRoot, "content", "chinese", "data", "pinyin", "syllables.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));

        var fromFile = doc.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("syllable").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        fromFile.Should().BeEquivalentTo(PinyinSyllableTable.Keys);
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
