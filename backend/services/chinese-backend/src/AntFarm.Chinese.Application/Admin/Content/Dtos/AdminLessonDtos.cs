using System.Text.Json;
using AntFarm.Chinese.Application.Lessons.Dtos;

namespace AntFarm.Chinese.Application.Admin.Content.Dtos;

/// <summary>Một khối nội dung trong <c>AdminLesson</c> (§6.3) — CÙNG hình dạng <c>LessonBlockDto</c> của học viên (F9), tách record riêng vì hai namespace khác mục đích tiến hoá độc lập.</summary>
public sealed record AdminLessonBlockDto(Guid Id, string Type, JsonElement Payload);

/// <summary>Một từ trong <c>AdminLesson</c> (§6.3) — admin cần thấy CẢ trạng thái duyệt nghĩa để biết từ nào chưa duyệt.</summary>
public sealed record AdminLessonWordDto(Guid Id, string Simplified, string Pinyin, IReadOnlyList<string> MeaningsVi, string MeaningViStatus);

/// <summary>Một lựa chọn quiz ĐẦY ĐỦ cho admin (khác <c>LessonQuizOptionDto</c> học viên — không ẩn gì).</summary>
public sealed record AdminQuizOptionDto(string Id, string Text, string Lang);

/// <summary>Một câu quiz ĐẦY ĐỦ cho admin (§6.3) — CÓ <c>correctOptionId</c>/<c>explanation</c>/<c>key</c> (khác DTO học viên R-LS10 chủ động ẩn).</summary>
public sealed record AdminQuizQuestionDto(
    Guid Id, string Key, string Type, string Prompt, string PromptLang, string? PromptPinyin, string? AudioText,
    IReadOnlyList<AdminQuizOptionDto> Options, string CorrectOptionId, string Explanation);

/// <summary>
/// Hình dạng dùng chung <c>AdminLesson</c> (§6.3) — trả về từ MỌI endpoint ghi (tạo/sửa/xuất
/// bản/gỡ/duyệt/lưu trữ/khôi phục) VÀ hai endpoint đọc (danh sách chi tiết, GET theo id).
/// <see cref="Version"/> = <c>xmin</c> đọc được — client gửi lại giá trị này ở lần ghi KẾ TIẾP
/// (R-CA3); <see cref="Warnings"/> tính LUÔN (không chỉ lúc xuất bản) để màn soạn hiện gợi ý sớm.
/// </summary>
public sealed record AdminLessonDto(
    Guid Id, uint Version, string Slug, string Title, string Topic, string Level, int OrderIndex,
    string Summary, IReadOnlyList<string> Objectives, short EstimatedMinutes, IReadOnlyList<LessonGlossaryItemDto> Glossary,
    string Status, string ReviewStatus, string Source,
    DateTime? PublishedAt, DateTime? ReviewedAt, DateTime? EditedAt, string? EditedByName,
    DateTime CreatedAt, DateTime UpdatedAt, bool HasAttempts,
    IReadOnlyList<AdminLessonBlockDto> Blocks, IReadOnlyList<AdminLessonWordDto> Words, IReadOnlyList<AdminQuizQuestionDto> Quiz,
    IReadOnlyList<string> Warnings);

/// <summary>Một dòng <c>GET /api/admin/lessons</c> (§6.3) — KHÔNG kèm khối/từ/quiz đầy đủ (chỉ đếm).</summary>
public sealed record AdminLessonListItemDto(
    Guid Id, string Slug, string Title, int OrderIndex, string Status, string ReviewStatus, string Source,
    int WordCount, int QuestionCount, DateTime? EditedAt, DateTime? PublishedAt);

public sealed record AdminLessonListResponseDto(IReadOnlyList<AdminLessonListItemDto> Items, int Page, int PageSize, int TotalCount);

/// <summary>Query string <c>GET /api/admin/lessons</c> (§6.3) — property có giá trị mặc định để thiếu tham số vẫn bind được.</summary>
public sealed class AdminLessonsQuery
{
    public string? Status { get; init; }
    public string? Q { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>Body <c>POST /api/admin/lessons</c> (§6.3) — <see cref="OrderIndex"/> bỏ trống ⇒ <c>max+1</c> (§5.2.3 "Tạo bài").</summary>
public sealed record CreateLessonRequest(string Slug, string Title, string Topic, int? OrderIndex, string? Summary);

/// <summary>Body <c>PUT /api/admin/lessons/{id}</c> (§6.3) — KHÔNG có <c>status</c>/<c>level</c> (đổi qua thao tác riêng/hằng số).</summary>
public sealed record UpdateLessonMetaRequest(
    uint Version, string Slug, string Title, string Topic, int OrderIndex, string Summary,
    IReadOnlyList<string> Objectives, short EstimatedMinutes, IReadOnlyList<LessonGlossaryItemDto> Glossary);

/// <summary>Một khối trong <c>PUT .../blocks</c> (§6.3) — <see cref="Payload"/> giữ nguyên <see cref="JsonElement"/>, kiểm bằng <c>LessonContentValidator</c> ở service (không deserialize sang record cụ thể).</summary>
public sealed record LessonBlockInputDto(string Type, JsonElement Payload);

public sealed record ReplaceLessonBlocksRequest(uint Version, IReadOnlyList<LessonBlockInputDto> Blocks);

public sealed record ReplaceLessonWordsRequest(uint Version, IReadOnlyList<Guid> WordIds);

/// <summary>Một lựa chọn trong <c>PUT .../quiz</c> (§6.3) — KHÔNG có <c>id</c> (server tự gán <c>a..d</c> theo VỊ TRÍ trong mảng, §6.3 ghi chú).</summary>
public sealed record QuizOptionInputDto(string Text, string Lang);

/// <summary>Một câu trong <c>PUT .../quiz</c> (§6.3) — <see cref="Id"/> có giá trị ⇒ cập nhật câu đã có (giữ nguyên <c>key</c>); <c>null</c> ⇒ tạo câu mới (<c>key = "m-" + 8 hex</c>). <see cref="CorrectIndex"/> 0-based, THAY <c>correctOptionId</c> để form không phải quản id lựa chọn.</summary>
public sealed record QuizQuestionInputDto(
    Guid? Id, string Type, string Prompt, string PromptLang, string? PromptPinyin, string? AudioText,
    IReadOnlyList<QuizOptionInputDto> Options, int CorrectIndex, string? Explanation);

public sealed record ReplaceLessonQuizRequest(uint Version, IReadOnlyList<QuizQuestionInputDto> Questions);

/// <summary>Body dùng chung cho <c>publish</c>/<c>unpublish</c>/<c>review</c>/<c>restore</c> (§6.3) — chỉ cần <c>version</c> (R-CA3).</summary>
public sealed record LessonVersionRequest(uint Version);

/// <summary>Kết quả <c>DELETE /api/admin/lessons/{id}</c> (§6.3, R-CA7) — <see cref="Lesson"/> chỉ có giá trị khi <see cref="HardDeleted"/> = <c>false</c> (chuyển <c>archived</c>).</summary>
public sealed record DeleteLessonResult(bool HardDeleted, AdminLessonDto? Lesson);
