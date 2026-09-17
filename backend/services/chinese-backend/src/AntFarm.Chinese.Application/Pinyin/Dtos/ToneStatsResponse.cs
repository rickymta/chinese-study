using System.Text.Json.Serialization;

namespace AntFarm.Chinese.Application.Pinyin.Dtos;

/// <summary>
/// Thống kê một thanh trong cửa sổ 200 phần gần nhất (R5-13). <see cref="Accuracy"/> đánh
/// <c>JsonIgnore(Never)</c> (RK41) — Program.cs bật <c>DefaultIgnoreCondition = WhenWritingNull</c>
/// toàn cục nên trường <c>null</c> mặc định bị LƯỢC BỎ khỏi JSON; frontend cần luôn thấy khoá
/// <c>accuracy</c> (kể cả khi <c>null</c>) để phân biệt "chưa có dữ liệu" với "thiếu trường".
/// </summary>
public sealed record ToneStatDto(int Total, int Correct, [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] double? Accuracy);

/// <summary>Một cặp thanh hay bị nhầm (R5-13) — vd nghe thanh 2 thành thanh 3.</summary>
public sealed record ConfusionDto(int Expected, int Answered, int Count);

/// <summary>Shape của GET /api/pinyin/tone-stats (§6.1).</summary>
public sealed record ToneStatsResponse(
    int TotalAnswered,
    int SessionsCount,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] DateTime? LastSessionAt,
    int WindowSize,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] double? Accuracy,
    IReadOnlyDictionary<string, ToneStatDto> ByTone,
    IReadOnlyList<ConfusionDto> Confusions,
    IReadOnlyList<int> RecommendedFocus,
    bool G0Reached);
