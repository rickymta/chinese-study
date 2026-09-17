namespace AntFarm.Chinese.Domain.Lessons;

/// <summary>Một từ thuộc bài học (§5.1.1) — khoá chính ghép <c>(LessonId, WordId)</c>, giữ thứ tự xuất hiện trong bài qua <see cref="OrderIndex"/>.</summary>
public sealed class LessonWord
{
    public Guid LessonId { get; private set; }
    public Guid WordId { get; private set; }
    public short OrderIndex { get; private set; }

    // EF Core cần constructor không tham số.
    private LessonWord()
    {
    }

    public static LessonWord Create(Guid lessonId, Guid wordId, short orderIndex) => new()
    {
        LessonId = lessonId,
        WordId = wordId,
        OrderIndex = orderIndex
    };

    /// <summary>F9.2.1.4 diff: cập nhật thứ tự khi file đổi vị trí từ mà không xoá/chèn lại dòng.</summary>
    public void SetOrderIndex(short orderIndex) => OrderIndex = orderIndex;
}
