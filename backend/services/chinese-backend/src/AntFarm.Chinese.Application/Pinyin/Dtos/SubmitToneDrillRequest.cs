using AntFarm.Chinese.Domain.Pinyin;

namespace AntFarm.Chinese.Application.Pinyin.Dtos;

/// <summary>Một phần của câu (một âm tiết) trong yêu cầu nộp bài — §6.1 POST /api/pinyin/tone-drills.</summary>
public sealed record SubmitToneDrillPartRequest(string Syllable, string Hanzi, int ExpectedTone, int AnsweredTone);

/// <summary>Một câu — 1 phần (<c>listen_tone</c>) hoặc 2 phần (<c>tone_pair</c>).</summary>
public sealed record SubmitToneDrillItemRequest(IReadOnlyList<SubmitToneDrillPartRequest> Parts, int? ResponseMs, int ReplayCount);

/// <summary>
/// Body POST /api/pinyin/tone-drills (§6.1). <see cref="StartedAt"/>/<see cref="FinishedAt"/> BẮT
/// BUỘC có offset/"Z" trong JSON — System.Text.Json chỉ gán <c>Kind=Utc</c> khi chuỗi có offset,
/// còn lại <c>Kind=Unspecified</c> (validator từ chối 400, D38 — tránh <c>Kind=Unspecified</c> lọt
/// vào Npgsql <c>timestamptz</c>).
/// </summary>
public sealed record SubmitToneDrillRequest(
    Guid ClientSessionId,
    ToneDrillMode Mode,
    DateTime StartedAt,
    DateTime FinishedAt,
    IReadOnlyList<SubmitToneDrillItemRequest> Items);

/// <summary>Đếm theo một thanh trong kết quả một phiên (đếm theo PHẦN, không phải câu — R5-12 chú thích §6.1).</summary>
public sealed record ToneCountDto(int Total, int Correct);

/// <summary>201 (mới)/200 (đã nộp trước đó, trả lại) của POST /api/pinyin/tone-drills (§6.1).</summary>
public sealed record SubmitToneDrillResponse(
    Guid Id,
    Guid ClientSessionId,
    ToneDrillMode Mode,
    int Total,
    int Correct,
    DateOnly LocalDate,
    IReadOnlyDictionary<string, ToneCountDto> ByTone);
