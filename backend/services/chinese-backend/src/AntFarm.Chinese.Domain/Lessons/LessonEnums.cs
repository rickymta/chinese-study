namespace AntFarm.Chinese.Domain.Lessons;

/// <summary>Trạng thái xuất bản của bài học (§5.1.1, R-LS1) — <c>Draft</c>: chỉ admin thấy; <c>Published</c>: học viên thấy; <c>Archived</c>: ẩn với học viên, admin xem ở bộ lọc "Lưu trữ" (chỉ xem).</summary>
public static class LessonStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Archived = "archived";

    public static readonly IReadOnlyList<string> All = [Draft, Published, Archived];
}

/// <summary>Trạng thái duyệt nội dung bài học (§5.1.1, R-LS2) — giống <c>MeaningViStatus</c> của F6 nhưng RIÊNG cho bài học (không dùng chung hằng số — hai khái niệm nghiệp vụ độc lập, đổi một bên không kéo bên kia).</summary>
public static class LessonReviewStatuses
{
    public const string Machine = "machine";
    public const string Reviewed = "reviewed";
}

/// <summary>Nguồn tạo bài học (§5.1.1) — <c>Seed</c>: nạp từ tệp học liệu (<see cref="LessonSources.Seed"/>, importer quản lý); <c>Admin</c>: tạo qua màn quản trị (F10).</summary>
public static class LessonSources
{
    public const string Seed = "seed";
    public const string Admin = "admin";
}

/// <summary>Loại khối nội dung bài học (§5.1.1).</summary>
public static class LessonBlockTypes
{
    public const string Text = "text";
    public const string Dialogue = "dialogue";
    public const string Grammar = "grammar";
    public const string Tip = "tip";

    public static readonly IReadOnlyList<string> All = [Text, Dialogue, Grammar, Tip];

    public static bool IsKnown(string type) => All.Contains(type, StringComparer.Ordinal);
}

/// <summary>Loại câu hỏi quiz (§5.1.1).</summary>
public static class QuizQuestionTypes
{
    public const string SingleChoice = "single_choice";
    public const string ListenChoice = "listen_choice";

    public static readonly IReadOnlyList<string> All = [SingleChoice, ListenChoice];

    public static bool IsKnown(string type) => All.Contains(type, StringComparer.Ordinal);
}
