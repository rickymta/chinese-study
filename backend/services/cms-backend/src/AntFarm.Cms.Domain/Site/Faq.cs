namespace AntFarm.Cms.Domain.Site;

/// <summary>
/// Một câu hỏi thường gặp hiển thị trên website (schema <c>site.faqs</c>, §5.1.2 W3b). Sắp xếp
/// theo nhóm (<see cref="GroupKey"/>, mặc định <c>general</c>) rồi <see cref="SortOrder"/> TRONG
/// nhóm đó. <see cref="Version"/> ánh xạ cột hệ thống <c>xmin</c> (khuôn <c>Language</c> W3a, R-CA3).
/// </summary>
public sealed class Faq
{
    public Guid Id { get; private set; }
    public string Question { get; private set; } = null!;
    public string AnswerMarkdown { get; private set; } = null!;
    public string GroupKey { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public uint Version { get; private set; }

    // EF Core cần constructor không tham số — không lộ ra ngoài assembly để buộc luôn tạo qua Create().
    private Faq()
    {
    }

    public static Faq Create(
        string question, string answerMarkdown, string groupKey, bool isPublished, int sortOrder,
        Guid actorId, DateTime now) => new()
    {
        Id = Guid.CreateVersion7(),
        Question = question,
        AnswerMarkdown = answerMarkdown,
        GroupKey = groupKey,
        SortOrder = sortOrder,
        IsPublished = isPublished,
        UpdatedAt = now,
        UpdatedBy = actorId
    };

    /// <summary>
    /// Sửa (§5.2.3 W3b). <paramref name="sortOrder"/> do <c>FaqAdminService</c> tính TRƯỚC khi gọi
    /// (giữ nguyên nếu <paramref name="groupKey"/> không đổi, đưa về cuối nhóm mới nếu đổi nhóm).
    /// </summary>
    public void Update(
        string question, string answerMarkdown, string groupKey, int sortOrder, bool isPublished,
        Guid actorId, DateTime now)
    {
        Question = question;
        AnswerMarkdown = answerMarkdown;
        GroupKey = groupKey;
        SortOrder = sortOrder;
        IsPublished = isPublished;
        UpdatedAt = now;
        UpdatedBy = actorId;
    }

    /// <summary>Đặt lại thứ tự hiển thị TRONG một nhóm (1..n) — dùng bởi <c>PUT /api/admin/faqs/order</c>.</summary>
    public void Reorder(int sortOrder) => SortOrder = sortOrder;
}
