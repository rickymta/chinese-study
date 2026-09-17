using System.Reflection;
using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Lessons;
using AntFarm.Chinese.Domain.Pinyin;
using AntFarm.Chinese.Domain.Srs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AntFarm.Chinese.Infrastructure.Persistence;

/// <summary>
/// F3: schema `access` (migration F3_Access, §5.1.2) — users, roles, permissions, user_roles,
/// role_permissions. F5: schema `learning` (migration F5_ToneDrill, §5.1.1) — study_events,
/// tone_drill_sessions, tone_drill_answers. F6: schema `content` (migration F6_Vocabulary, §5.1.1)
/// — words, characters, word_characters, import_runs; bật extension <c>pg_trgm</c> (RK39: role
/// af_chinese là OWNER của DB af_chinese, extension trusted nên tự CREATE EXTENSION được, không
/// cần superuser). F7: schema `learning` (migration F7_Srs, §5.1.2) — srs_cards, srs_review_logs,
/// learner_settings. F9: schema `content`/`learning` (migration F9_Lessons, §5.1.1) — bài học, khối,
/// từ của bài, quiz, tiến độ, lần nộp quiz. KHÔNG gọi <c>HasDefaultSchema</c> — mỗi cấu hình tự khai
/// schema riêng.
/// </summary>
public sealed class ChineseDbContext(DbContextOptions<ChineseDbContext> options)
    : DbContext(options), IChineseDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<StudyEvent> StudyEvents => Set<StudyEvent>();
    public DbSet<ToneDrillSession> ToneDrillSessions => Set<ToneDrillSession>();
    public DbSet<ToneDrillAnswer> ToneDrillAnswers => Set<ToneDrillAnswer>();

    public DbSet<Word> Words => Set<Word>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<WordCharacter> WordCharacters => Set<WordCharacter>();
    public DbSet<ImportRun> ImportRuns => Set<ImportRun>();

    public DbSet<SrsCard> SrsCards => Set<SrsCard>();
    public DbSet<SrsReviewLog> SrsReviewLogs => Set<SrsReviewLog>();
    public DbSet<LearnerSettings> LearnerSettings => Set<LearnerSettings>();

    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonBlock> LessonBlocks => Set<LessonBlock>();
    public DbSet<LessonWord> LessonWords => Set<LessonWord>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public void ClearTracking() => ChangeTracker.Clear();

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => await Database.BeginTransactionAsync(cancellationToken);

    public Task<int> ExecuteSqlAsync(FormattableString sql, CancellationToken cancellationToken = default)
        => Database.ExecuteSqlInterpolatedAsync(sql, cancellationToken);
}
