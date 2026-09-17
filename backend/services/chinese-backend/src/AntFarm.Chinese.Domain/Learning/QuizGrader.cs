namespace AntFarm.Chinese.Domain.Learning;

/// <summary>Kết quả chấm một câu (§5.2.1.1) — <see cref="OptionId"/> là lựa chọn học viên đã chọn.</summary>
public sealed record GradedAnswer(Guid QuestionId, string OptionId, bool Correct, string CorrectOptionId);

/// <summary>Kết quả chấm toàn bài quiz (R-LS3) — <see cref="Passed"/> tính bằng số nguyên, KHÔNG làm tròn (D7).</summary>
public sealed record QuizGrade(int Total, int Correct, int ScorePercent, bool Passed, IReadOnlyList<GradedAnswer> Items);

/// <summary>
/// Hàm thuần chấm quiz (§5.2.1.1) — không phụ thuộc DB/HTTP, test trực tiếp không cần dựng gì thêm.
/// Ngưỡng đạt <see cref="PassThresholdPercent"/> = 80 (R-LS3): <c>correct*100 >= 80*total</c> (vd 4/5
/// = 80 đạt; 7/9 ≈ 77.7 không đạt); <see cref="QuizGrade.ScorePercent"/> hiển thị = floor(correct*100/total).
/// </summary>
public static class QuizGrader
{
    public const int PassThresholdPercent = 80;

    /// <exception cref="ArgumentException">Tập <c>questionId</c> trong <paramref name="answers"/> khác tập câu hỏi hiện có của bài — Application phải tự đổi thành lỗi nghiệp vụ <c>QUIZ_CHANGED</c> TRƯỚC khi gọi hàm này (R-LS8), không để ngoại lệ này lộ ra client.</exception>
    public static QuizGrade Grade(IReadOnlyList<LessonQuizQuestionSnapshot> questions, IReadOnlyDictionary<Guid, string> answers)
    {
        var questionIds = questions.Select(q => q.Id).ToHashSet();
        if (questionIds.Count != answers.Count || answers.Keys.Any(id => !questionIds.Contains(id)))
            throw new ArgumentException("Tập câu hỏi trong bài nộp không khớp bộ câu hỏi hiện tại của bài.", nameof(answers));

        var items = new List<GradedAnswer>(questions.Count);
        var correct = 0;
        foreach (var question in questions)
        {
            var optionId = answers[question.Id];
            var isCorrect = string.Equals(optionId, question.CorrectOptionId, StringComparison.Ordinal);
            if (isCorrect)
                correct++;

            items.Add(new GradedAnswer(question.Id, optionId, isCorrect, question.CorrectOptionId));
        }

        var total = questions.Count;
        var scorePercent = total == 0 ? 0 : correct * 100 / total; // chia nguyên = floor cho số không âm
        var passed = total > 0 && correct * 100 >= PassThresholdPercent * total;

        return new QuizGrade(total, correct, scorePercent, passed, items);
    }
}

/// <summary>Ảnh chụp tối thiểu một câu quiz cần để chấm (§5.2.1.1) — tách khỏi entity <c>QuizQuestion</c> (Domain.Lessons) để <c>QuizGrader</c> không phải biết jsonb/Options.</summary>
public sealed record LessonQuizQuestionSnapshot(Guid Id, string CorrectOptionId);
