using AntFarm.Chinese.Domain.Learning;

namespace AntFarm.Chinese.Application.Learning;

/// <summary>Ghi một dòng <c>learning.study_events</c> (§5.1.1) — dùng chung mọi loại bài tập (F5 tone_drill, F7 srs_review, F8 writing, F9 quiz_submit/lesson_complete).</summary>
public interface IStudyActivityRecorder
{
    /// <summary>
    /// KHÔNG tự gọi <c>SaveChangesAsync</c> — người gọi (vd <c>ToneDrillService</c>) lưu trong
    /// transaction của chính họ (R5-12: session + answers + study_event trong CÙNG một lượt ghi).
    /// </summary>
    Task<StudyEvent> RecordAsync(Guid userId, string kind, DateTime occurredAtUtc, int quantity, int? correct, Guid? refId, CancellationToken ct);
}
