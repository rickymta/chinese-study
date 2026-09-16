using AntFarm.Chinese.Application.Pinyin.Dtos;

namespace AntFarm.Chinese.Application.Pinyin;

/// <summary>
/// Cổng đọc học liệu pinyin đã nạp lúc khởi động (§5.2.1) — hiện thực ở Infrastructure
/// (<c>PinyinCatalogLoader</c>/<c>PinyinCatalog</c>, đọc <c>content/chinese/data/pinyin/*.json</c>).
/// Nạp lỗi (thiếu file, JSON hỏng, vi phạm quy tắc) ⇒ <see cref="IsAvailable"/> = false — KHÔNG
/// làm service sập, chỉ endpoint pinyin trả 503 <c>CONTENT_UNAVAILABLE</c>.
/// </summary>
public interface IPinyinCatalog
{
    bool IsAvailable { get; }

    /// <summary>16 ký tự hex SHA-256 của nội dung 4 file học liệu — dùng làm ETag (§6.1).</summary>
    string Version { get; }

    /// <summary>Ném <see cref="AntFarm.Core.Errors.ServiceUnavailableException"/> (code <c>CONTENT_UNAVAILABLE</c>) khi <see cref="IsAvailable"/> = false.</summary>
    PinyinChartDto Chart { get; }

    /// <summary>Ném <see cref="AntFarm.Core.Errors.ServiceUnavailableException"/> (code <c>CONTENT_UNAVAILABLE</c>) khi <see cref="IsAvailable"/> = false.</summary>
    PinyinGuideDto Guide { get; }

    bool TryGetToneExample(string syllable, int tone, out ToneExampleDto? example);

    bool ContainsSyllable(string syllable);
}
