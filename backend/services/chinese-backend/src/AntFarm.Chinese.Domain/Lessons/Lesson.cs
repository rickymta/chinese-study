namespace AntFarm.Chinese.Domain.Lessons;

/// <summary>
/// Một bài học chủ đề HSK 1 (§5.1.1, migration F9_Lessons) — nạp từ
/// <c>content/chinese/data/lessons/NN-slug.json</c> bởi <c>LessonImporter</c> (§5.2.1.4, R-LS14) hoặc
/// tạo qua quản trị (F10). Khoá tự nhiên <c>Slug</c>.
/// </summary>
public sealed class Lesson
{
    public Guid Id { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Topic { get; private set; } = null!;
    public string Level { get; private set; } = "hsk1";
    public int OrderIndex { get; private set; }
    public string Summary { get; private set; } = "";
    public List<string> Objectives { get; private set; } = [];
    public short EstimatedMinutes { get; private set; } = 15;

    /// <summary>jsonb — mảng <see cref="GlossaryItem"/> đã tuần tự hoá (Application dùng <c>LessonJson</c>).</summary>
    public string Glossary { get; private set; } = "[]";

    public string Status { get; private set; } = LessonStatuses.Draft;
    public string ReviewStatus { get; private set; } = LessonReviewStatuses.Machine;
    public string Source { get; private set; } = LessonSources.Seed;

    /// <summary>SHA-256 hex của tệp JSON nguồn (seed); <c>null</c> nếu bài tạo qua quản trị (R-LS14).</summary>
    public string? SourceHash { get; private set; }

    /// <summary>Lần xuất bản ĐẦU TIÊN — không xoá khi gỡ xuất bản (R-LS1).</summary>
    public DateTime? PublishedAt { get; private set; }

    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public Guid? CreatedBy { get; private set; }

    /// <summary>Lần ghi GẦN NHẤT của admin (F10) — có giá trị ⇒ importer KHÔNG nạp đè (R-LS14).</summary>
    public DateTime? EditedAt { get; private set; }
    public Guid? EditedBy { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Concurrency token ánh xạ cột hệ thống <c>xmin</c> (Npgsql <c>IsRowVersion</c>, §5.1) — F10 dùng cho <c>version</c> chống ghi đè đồng thời (R-CA3).</summary>
    public uint Version { get; private set; }

    // EF Core cần constructor không tham số.
    private Lesson()
    {
    }

    public bool IsPublished => Status == LessonStatuses.Published;

    /// <summary>Bài chưa sửa/duyệt tay ⇒ lượt nạp học liệu tiếp theo được phép cập nhật tại chỗ (R-LS14).</summary>
    public bool IsSeedImportable => EditedAt is null && ReviewStatus == LessonReviewStatuses.Machine;

    /// <summary>Chèn bài mới từ tệp học liệu (R-LS14 "Chưa có slug") — <c>status</c> lấy theo tệp, mặc định xuất bản ngay ⇒ <see cref="PublishedAt"/> = <paramref name="nowUtc"/>.</summary>
    public static Lesson CreateSeed(LessonImportData data, string sourceHash, DateTime nowUtc)
    {
        var lesson = new Lesson
        {
            Id = Guid.CreateVersion7(),
            Source = LessonSources.Seed,
            ReviewStatus = LessonReviewStatuses.Machine,
            SourceHash = sourceHash,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };

        lesson.ApplyFields(data);
        lesson.Status = data.Status;
        if (lesson.IsPublished)
            lesson.PublishedAt = nowUtc;

        return lesson;
    }

    /// <summary>
    /// Cập nhật tại chỗ từ tệp học liệu đã đổi (R-LS14 "source_hash khác") — CHỈ gọi khi
    /// <see cref="IsSeedImportable"/>. GIỮ NGUYÊN <see cref="Status"/> (chỉ admin F10 đổi trạng thái
    /// xuất bản) và <see cref="PublishedAt"/> — importer không tự xuất bản/gỡ bài.
    /// </summary>
    public void ApplySeedUpdate(LessonImportData data, string sourceHash, DateTime nowUtc)
    {
        ApplyFields(data);
        SourceHash = sourceHash;
        UpdatedAt = nowUtc;
    }

    private void ApplyFields(LessonImportData data)
    {
        Slug = data.Slug;
        Title = data.Title;
        Topic = data.Topic;
        Level = data.Level;
        OrderIndex = data.OrderIndex;
        Summary = data.Summary;
        Objectives = [.. data.Objectives];
        EstimatedMinutes = data.EstimatedMinutes;
        Glossary = data.GlossaryJson;
    }

    /// <summary>F10: đặt dấu vết sửa tay gần nhất — chạm dòng để <c>xmin</c> đổi kể cả khi chỉ sửa bảng con (R-CA2).</summary>
    public void TouchEdited(Guid userId, DateTime nowUtc)
    {
        EditedAt = nowUtc;
        EditedBy = userId;
        UpdatedAt = nowUtc;
    }
}
