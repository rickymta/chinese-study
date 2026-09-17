namespace AntFarm.Chinese.Domain.Lessons;

/// <summary>
/// Trường "đầu bài" (không gồm blocks/words/quiz — ba bảng con importer xử lý riêng) đã kiểm hợp lệ,
/// sẵn sàng nạp vào <see cref="Lesson"/> (§5.2.1.4) — cùng mẫu <c>WordImportData</c>/<c>CharacterImportData</c> của F6.
/// </summary>
public sealed record LessonImportData(
    string Slug,
    string Title,
    string Topic,
    string Level,
    int OrderIndex,
    string Summary,
    IReadOnlyList<string> Objectives,
    short EstimatedMinutes,
    string GlossaryJson,
    string Status);
