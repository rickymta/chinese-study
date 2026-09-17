using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Lessons;
using AntFarm.Chinese.Domain.Pinyin;
using AntFarm.Chinese.Domain.Srs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AntFarm.Chinese.Application.Common.Abstractions;

/// <summary>
/// Application chỉ phụ thuộc interface này, không phụ thuộc thẳng EF Core DbContext của
/// Infrastructure (DDD 4 lớp). F3 bổ sung DbSet của schema `access` (§5.1.2); F5 bổ sung schema
/// `learning` (§5.1.1) + <see cref="BeginTransactionAsync"/> (nộp bài luyện thanh ghi
/// session + answers + study_event trong CÙNG một transaction, R5-12). F6 bổ sung schema `content`
/// (§5.1.1) — từ vựng, chữ Hán, nhật ký nạp học liệu. F7 bổ sung thẻ SRS + cài đặt học tập
/// (§5.1.2, schema `learning`). F9 bổ sung bài học + quiz (schema `content`/`learning`, migration
/// F9_Lessons).
/// </summary>
public interface IChineseDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }

    DbSet<StudyEvent> StudyEvents { get; }
    DbSet<ToneDrillSession> ToneDrillSessions { get; }
    DbSet<ToneDrillAnswer> ToneDrillAnswers { get; }

    DbSet<Word> Words { get; }
    DbSet<Character> Characters { get; }
    DbSet<WordCharacter> WordCharacters { get; }
    DbSet<ImportRun> ImportRuns { get; }

    DbSet<SrsCard> SrsCards { get; }
    DbSet<SrsReviewLog> SrsReviewLogs { get; }
    DbSet<LearnerSettings> LearnerSettings { get; }

    DbSet<Lesson> Lessons { get; }
    DbSet<LessonBlock> LessonBlocks { get; }
    DbSet<LessonWord> LessonWords { get; }
    DbSet<QuizQuestion> QuizQuestions { get; }
    DbSet<LessonProgress> LessonProgress { get; }
    DbSet<QuizAttempt> QuizAttempts { get; }

    DbSet<WritingAttempt> WritingAttempts { get; }
    DbSet<CharacterWritingStats> CharacterWritingStats { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Chạy một câu lệnh SQL tham số hoá qua interpolated string (KHÔNG <c>FromSqlRaw</c> nối chuỗi
    /// tay) — dùng cho <c>INSERT ... ON CONFLICT DO NOTHING</c> khi hai request đua nhau tạo cùng
    /// một khoá duy nhất (F7: <c>ux_srs_cards_user_word_type</c>, review F7.1 17/09/2026 — trước đó
    /// dùng <c>Add()</c> + <c>SaveChangesAsync</c> nên đụng độ ném <c>23505</c> ⇒ 500) hoặc các câu
    /// lệnh ghi khác không cần theo dõi qua ChangeTracker. Tự tham gia transaction hiện tại của
    /// DbContext nếu người gọi đã <see cref="BeginTransactionAsync"/> trước đó. Trả số dòng bị ảnh hưởng.
    /// </summary>
    Task<int> ExecuteSqlAsync(FormattableString sql, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gỡ TOÀN BỘ entity đang theo dõi khỏi ChangeTracker — dùng khi một lượt ghi thất bại do
    /// đụng độ (race, vd hai request cùng provision một user lần đầu) và code cần đọc lại trạng
    /// thái THẬT từ DB. Không gọi thì entity vừa <c>Add()</c> còn ở trạng thái <c>Added</c> trong
    /// DbContext (scoped/một request) ⇒ lượt <c>SaveChangesAsync</c> KẾ TIẾP trong CÙNG request
    /// (vd controller khác) sẽ cố chèn lại chúng ⇒ lỗi 500 (review F3 17/09/2026).
    /// </summary>
    void ClearTracking();

    /// <summary>
    /// F10 (R-CA3): gán <c>OriginalValue</c> của thuộc tính concurrency token <c>Version</c> (ánh xạ
    /// <c>xmin</c>, <see cref="Microsoft.EntityFrameworkCore.PropertyBuilder.IsRowVersion"/>) bằng
    /// giá trị người gọi gửi lên — <c>SaveChangesAsync</c> sinh <c>UPDATE ... WHERE xmin = @original</c>;
    /// lệch (đã bị sửa ở nơi khác) ⇒ 0 dòng ảnh hưởng ⇒ EF ném
    /// <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/> (Application dịch
    /// thành <c>409 CONCURRENCY_CONFLICT</c>). Dùng chung cho <c>Lesson</c>/<c>Word</c> — cả hai đặt
    /// TÊN thuộc tính GIỐNG NHAU ("Version") nên không cần generic theo kiểu cụ thể.
    /// </summary>
    void SetOriginalVersion(object entity, uint version);
}
