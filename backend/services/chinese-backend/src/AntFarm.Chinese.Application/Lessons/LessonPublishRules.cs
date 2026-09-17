namespace AntFarm.Chinese.Application.Lessons;

/// <summary>Một câu quiz (dạng rút gọn) đủ dữ liệu để kiểm điều kiện xuất bản (R-CA4) — tách khỏi entity để dùng được TRƯỚC khi bài tồn tại trong DB (importer, F9).</summary>
public sealed record PublishCheckQuestion(string Type, int OptionCount, bool CorrectOptionInOptions, string? AudioText);

/// <summary>Đầu vào kiểm điều kiện xuất bản một bài (R-CA4) — dùng chung cho <c>LessonImporter</c> (F9, bài <c>published</c> từ tệp) và quản trị (F10, xuất bản tay).</summary>
public sealed record PublishCheckInput(
    string Title, int BlockCount, bool HasDialogueBlock, int WordCount, IReadOnlyList<PublishCheckQuestion> Questions);

/// <summary>
/// Điều kiện xuất bản một bài học (R-CA4, §5.2.1.2) — hàm thuần, không phụ thuộc DB/HTTP.
/// <see cref="Check"/> trả <c>problems</c> (chặn xuất bản, <c>422 LESSON_NOT_PUBLISHABLE</c>) và
/// <c>warnings</c> (không chặn — F10 hiển thị cảnh báo, F9 importer log Warning nhưng vẫn nạp).
/// </summary>
public static class LessonPublishRules
{
    private const int MinQuestions = 3;
    private const int RecommendedQuestions = 5;
    private const double RecommendedListenShare = 0.3;
    private const int RecommendedMaxWords = 15;

    public static (IReadOnlyList<string> Problems, IReadOnlyList<string> Warnings) Check(PublishCheckInput input)
    {
        var problems = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(input.Title))
            problems.Add("Tiêu đề không được rỗng.");

        if (input.BlockCount < 1)
            problems.Add("Bài phải có ít nhất 1 khối nội dung.");

        if (input.WordCount < 1)
            problems.Add("Bài phải có ít nhất 1 từ.");

        if (input.Questions.Count < MinQuestions)
            problems.Add($"Bài phải có ít nhất {MinQuestions} câu quiz (hiện có {input.Questions.Count}).");

        foreach (var question in input.Questions)
        {
            if (question.OptionCount is < 2 or > 4)
                problems.Add("Mỗi câu quiz phải có 2–4 lựa chọn.");

            if (!question.CorrectOptionInOptions)
                problems.Add("correctOptionId phải thuộc danh sách lựa chọn của câu.");

            if (question.Type == "listen_choice" && string.IsNullOrEmpty(question.AudioText))
                problems.Add("Câu dạng nghe (listen_choice) phải có audioText.");
        }

        if (input.Questions.Count > 0 && input.Questions.Count < RecommendedQuestions)
            warnings.Add($"Chỉ có {input.Questions.Count} câu quiz (khuyến nghị ≥ {RecommendedQuestions}).");

        if (input.Questions.Count > 0)
        {
            var listenCount = input.Questions.Count(q => q.Type == "listen_choice");
            if ((double)listenCount / input.Questions.Count < RecommendedListenShare)
                warnings.Add($"Chỉ {listenCount}/{input.Questions.Count} câu nghe (khuyến nghị ≥ 30%).");
        }

        if (input.WordCount > RecommendedMaxWords)
            warnings.Add($"Bài có {input.WordCount} từ (khuyến nghị ≤ {RecommendedMaxWords}).");

        if (!input.HasDialogueBlock)
            warnings.Add("Bài không có khối hội thoại (dialogue).");

        return (problems, warnings);
    }
}
