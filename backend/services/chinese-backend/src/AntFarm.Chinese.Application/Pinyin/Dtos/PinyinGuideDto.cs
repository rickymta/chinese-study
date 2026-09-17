using System.Text.Json;

namespace AntFarm.Chinese.Application.Pinyin.Dtos;

/// <summary>
/// Một chủ đề hướng dẫn (§5.4.2, §6.1). <see cref="Blocks"/> giữ NGUYÊN VĂN mảng JSON gốc của
/// <c>guide.json</c> (kiểu block đa hình: paragraph/tone_contour/examples/compare/tip) — Application
/// không cần hiểu cấu trúc từng loại block, chỉ chuyển tiếp cho frontend hiển thị đúng như đã kiểm
/// bởi <c>content/chinese/scripts/validate.mjs</c>.
/// </summary>
public sealed record GuideTopicDto(string Id, string Title, int Order, JsonElement Blocks);

/// <summary>Shape của GET /api/pinyin/guide (§6.1).</summary>
public sealed record PinyinGuideDto(string Version, IReadOnlyList<GuideTopicDto> Topics);
