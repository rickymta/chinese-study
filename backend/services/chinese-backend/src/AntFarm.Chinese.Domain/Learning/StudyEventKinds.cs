namespace AntFarm.Chinese.Domain.Learning;

/// <summary>
/// Danh mục loại hoạt động học ghi vào <c>learning.study_events</c> (§5.1.1) — bảng KHÔNG có CHECK
/// constraint cho <c>kind</c> (D31: feature sau thêm loại không phải sửa constraint DB), việc kiểm
/// "kind đã biết chưa" nằm ở tầng code (<c>IStudyActivityRecorder</c>).
/// </summary>
public static class StudyEventKinds
{
    /// <summary>F5: một phiên luyện nghe-chọn thanh đã nộp xong.</summary>
    public const string ToneDrill = "tone_drill";

    /// <summary>F7: một lượt ôn tập SRS.</summary>
    public const string SrsReview = "srs_review";

    /// <summary>F8: một lượt luyện viết.</summary>
    public const string Writing = "writing";

    /// <summary>F9: nộp một bài quiz.</summary>
    public const string QuizSubmit = "quiz_submit";

    /// <summary>F9: hoàn thành một bài học.</summary>
    public const string LessonComplete = "lesson_complete";

    private static readonly HashSet<string> Known = [ToneDrill, SrsReview, Writing, QuizSubmit, LessonComplete];

    public static bool IsKnown(string kind) => Known.Contains(kind);
}
