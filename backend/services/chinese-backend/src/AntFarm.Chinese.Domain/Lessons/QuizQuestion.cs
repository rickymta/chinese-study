namespace AntFarm.Chinese.Domain.Lessons;

/// <summary>
/// Một câu quiz của bài học (§5.1.1) — <see cref="Key"/> là khoá ổn định dùng để upsert lúc nạp lại
/// học liệu (R-LS14: giữ nguyên <see cref="Id"/> câu cũ khi chỉ nội dung đổi, để lịch sử
/// <c>quiz_attempts.answers</c> cũ vẫn còn ý nghĩa tham chiếu). Đáp án (<see cref="CorrectOptionId"/>)
/// và <see cref="Explanation"/> KHÔNG BAO GIỜ trả cho học viên trước khi nộp bài (R-LS10) — DTO chi
/// tiết bài học (Application) tự lọc, không dựa vào entity này để "quên" ẩn.
/// </summary>
public sealed class QuizQuestion
{
    public Guid Id { get; private set; }
    public Guid LessonId { get; private set; }
    public string Key { get; private set; } = null!;
    public short OrderIndex { get; private set; }
    public string Type { get; private set; } = null!;
    public string Prompt { get; private set; } = null!;
    public string PromptLang { get; private set; } = null!;
    public string? PromptPinyin { get; private set; }
    public string? AudioText { get; private set; }

    /// <summary>jsonb — mảng <see cref="QuizOption"/> đã tuần tự hoá.</summary>
    public string Options { get; private set; } = null!;

    public string CorrectOptionId { get; private set; } = null!;
    public string Explanation { get; private set; } = "";

    // EF Core cần constructor không tham số.
    private QuizQuestion()
    {
    }

    public static QuizQuestion Create(
        Guid lessonId, string key, short orderIndex, string type, string prompt, string promptLang,
        string? promptPinyin, string? audioText, string optionsJson, string correctOptionId, string explanation) => new()
    {
        Id = Guid.CreateVersion7(),
        LessonId = lessonId,
        Key = key,
        OrderIndex = orderIndex,
        Type = type,
        Prompt = prompt,
        PromptLang = promptLang,
        PromptPinyin = promptPinyin,
        AudioText = audioText,
        Options = optionsJson,
        CorrectOptionId = correctOptionId,
        Explanation = explanation
    };

    /// <summary>Cập nhật tại chỗ (upsert theo <see cref="Key"/>, R-LS14) — GIỮ NGUYÊN <see cref="Id"/>/<see cref="Key"/>.</summary>
    public void Apply(
        short orderIndex, string type, string prompt, string promptLang,
        string? promptPinyin, string? audioText, string optionsJson, string correctOptionId, string explanation)
    {
        OrderIndex = orderIndex;
        Type = type;
        Prompt = prompt;
        PromptLang = promptLang;
        PromptPinyin = promptPinyin;
        AudioText = audioText;
        Options = optionsJson;
        CorrectOptionId = correctOptionId;
        Explanation = explanation;
    }
}
