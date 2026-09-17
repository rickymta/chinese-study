using System.Text;
using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Common.Time;
using AntFarm.Chinese.Application.Learning;
using AntFarm.Chinese.Application.Writing.Dtos;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Writing;

/// <summary>
/// <c>POST /api/writing/attempts</c> + <c>GET /api/writing/summary</c> (§5.2.2, §6.2) — ghi MỘT LẦN
/// VIẾT hoàn tất, idempotent theo <c>clientAttemptId</c> (R-W8, cùng kỹ thuật
/// <c>SrsReviewService</c>/<c>QuizSubmissionService</c>).
/// </summary>
public sealed class WritingService(
    IChineseDbContext db,
    IUserDayContext userDayContext,
    IStudyActivityRecorder studyActivityRecorder)
{
    public async Task<(RecordWritingAttemptResponseDto Response, bool IsReplay)> RecordAttemptAsync(
        Guid userId, RecordWritingAttemptRequest request, CancellationToken ct)
    {
        // Chuẩn hoá NFC MỘT LẦN tại cửa vào (cùng quy ước RecordWritingAttemptValidator/
        // CjkCharacterValidation và WritingController.GetCharacter) — content.characters.hanzi lưu
        // dạng NFC (ContentImporter chuẩn hoá lúc nạp). Một chữ Hán TƯƠNG THÍCH (compatibility
        // ideograph, vd U+F900 蓋 NFC-normalize về U+76D6) gửi lên KHÔNG qua Normalize sẽ tra nhầm
        // ra 422 UNKNOWN_CHARACTER dù về mặt hiển thị/khoá tự nhiên là CÙNG một chữ — request đã qua
        // FluentValidation (IsSingleCjkCharacter tự bọc try/catch Normalize) nên gọi thẳng ở đây an
        // toàn, không ném với chuỗi dị dạng.
        request = request with { Hanzi = request.Hanzi.Normalize(NormalizationForm.FormC) };

        var existingAttempt = await db.WritingAttempts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.ClientAttemptId == request.ClientAttemptId, ct);
        if (existingAttempt is not null)
        {
            EnsureSameUser(existingAttempt, userId);
            return (await BuildReplayResponseAsync(existingAttempt, ct), true);
        }

        try
        {
            return (await RecordNewAsync(userId, request, ct), false);
        }
        catch (DbUpdateException ex) when (IsClientAttemptIdUniqueViolation(ex))
        {
            // Đua hiếm (R-W8): hai request cùng clientAttemptId gần như đồng thời — transaction của
            // ta tự ROLLBACK khi ném (chưa Commit); gỡ ChangeTracker rồi đọc lại kết quả người thắng
            // (cùng kỹ thuật SrsReviewService.ReviewAsync/QuizSubmissionService.SubmitAsync).
            db.ClearTracking();

            var winner = await db.WritingAttempts.AsNoTracking()
                .FirstOrDefaultAsync(a => a.ClientAttemptId == request.ClientAttemptId, ct)
                ?? throw new InvalidOperationException("Vi phạm duy nhất client_attempt_id nhưng không tìm thấy attempt tương ứng — không nên xảy ra.");

            EnsureSameUser(winner, userId);
            return (await BuildReplayResponseAsync(winner, ct), true);
        }
    }

    private async Task<RecordWritingAttemptResponseDto> RecordNewAsync(Guid userId, RecordWritingAttemptRequest request, CancellationToken ct)
    {
        // R-W7: chữ không có trong content.characters ⇒ 422 UNKNOWN_CHARACTER (server KHÔNG kiểm
        // totalStrokes so với stroke_count — Unihan và Make Me a Hanzi có thể lệch).
        var characterExists = await db.Characters.AsNoTracking().AnyAsync(c => c.Hanzi == request.Hanzi, ct);
        if (!characterExists)
            throw new BusinessRuleException("UNKNOWN_CHARACTER", $"Không tìm thấy chữ '{request.Hanzi}' trong kho học liệu.");

        var day = await userDayContext.GetAsync(userId, ct);

        await using var transaction = await db.BeginTransactionAsync(ct);

        // Tạo dòng thống kê RỖNG nếu chưa có — ghi NGAY qua SQL (ON CONFLICT DO NOTHING) rồi SELECT
        // ... FOR UPDATE để khoá đúng dòng (cùng kỹ thuật SrsCardService.InsertNewCardIfMissingAsync):
        // hai lần viết CÙNG một chữ gửi lên gần như đồng thời không làm mất số đếm của nhau.
        await db.ExecuteSqlAsync($"""
            INSERT INTO learning.character_writing_stats
                (user_id, hanzi, attempts, guided_attempts, recall_attempts, last_mode, last_mistakes, last_hints,
                 clean_recall_days, first_practiced_at, last_practiced_at)
            VALUES ({userId}, {request.Hanzi}, 0, 0, 0, {WritingModes.Guided}, 0, 0, 0, {day.NowUtc}, {day.NowUtc})
            ON CONFLICT (user_id, hanzi) DO NOTHING
            """, ct);

        var stats = await db.CharacterWritingStats
            .FromSqlInterpolated($"SELECT * FROM learning.character_writing_stats WHERE user_id = {userId} AND hanzi = {request.Hanzi} FOR UPDATE")
            .FirstAsync(ct);

        var wasMastered = stats.MasteryStatus == MasteryStatuses.Mastered;

        var attempt = WritingAttempt.Create(
            request.ClientAttemptId, userId, request.Hanzi, request.Mode,
            request.TotalStrokes, request.TotalMistakes, request.HintsUsed, request.DurationMs,
            day.NowUtc, day.LocalDate);
        db.WritingAttempts.Add(attempt);

        stats.Apply(attempt, day.LocalDate);

        // R-W4: correct = 1/0 chỉ cho recall (IsClean đã bao hàm điều kiện mode=recall); guided ⇒ NULL.
        await studyActivityRecorder.RecordAsync(
            userId, StudyEventKinds.Writing, day.NowUtc, 1,
            request.Mode == WritingModes.Recall ? (attempt.IsClean ? 1 : 0) : null,
            attempt.Id, ct);

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var becameMastered = !wasMastered && stats.MasteryStatus == MasteryStatuses.Mastered;

        return new RecordWritingAttemptResponseDto(attempt.Id, attempt.CompletedAt, attempt.IsClean, ToStatsDto(stats), becameMastered);
    }

    /// <summary>Gửi lại đúng <c>clientAttemptId</c> (R-W8) — dựng lại kết quả từ số liệu ĐÃ LƯU, KHÔNG ghi thêm gì; <c>becameMastered</c> luôn <c>false</c> (không phải một lần chuyển trạng thái MỚI).</summary>
    private async Task<RecordWritingAttemptResponseDto> BuildReplayResponseAsync(WritingAttempt attempt, CancellationToken ct)
    {
        var stats = await db.CharacterWritingStats.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == attempt.UserId && s.Hanzi == attempt.Hanzi, ct)
            ?? throw new InvalidOperationException("Có writing_attempts nhưng thiếu character_writing_stats tương ứng — không nên xảy ra.");

        return new RecordWritingAttemptResponseDto(attempt.Id, attempt.CompletedAt, attempt.IsClean, ToStatsDto(stats), BecameMastered: false);
    }

    /// <summary><c>clientAttemptId</c> trùng nhưng KHÁC người dùng ⇒ 409 <c>DUPLICATE_ATTEMPT_ID</c> thay vì âm thầm trả kết quả của người kia (cùng nguyên tắc F9).</summary>
    private static void EnsureSameUser(WritingAttempt existing, Guid callerId)
    {
        if (existing.UserId != callerId)
            throw new ConflictException("DUPLICATE_ATTEMPT_ID", "Mã lần viết (clientAttemptId) đã được dùng cho người dùng khác.");
    }

    public async Task<WritingSummaryDto> GetSummaryAsync(Guid userId, CancellationToken ct)
    {
        var stats = await db.CharacterWritingStats.AsNoTracking().Where(s => s.UserId == userId).ToListAsync(ct);
        var practicedChars = stats.Count;
        var masteredChars = stats.Count(s => s.MasteryStatus == MasteryStatuses.Mastered);
        var weakChars = stats.Count(s => s.IsWeak);

        var day = await userDayContext.GetAsync(userId, ct);
        var attemptsToday = await db.WritingAttempts.AsNoTracking()
            .CountAsync(a => a.UserId == userId && a.LocalDate == day.LocalDate, ct);

        var totalChars = (await Hsk1CharacterSet.GetOrderedHanziAsync(db, ct)).Count;

        return new WritingSummaryDto(practicedChars, masteredChars, weakChars, attemptsToday, totalChars);
    }

    internal static CharacterWritingStatsDto ToStatsDto(CharacterWritingStats s) => new(
        s.Hanzi, s.Attempts, s.GuidedAttempts, s.RecallAttempts, s.LastMistakes, s.LastHints,
        s.BestRecallMistakes, s.CleanRecallDays, s.MasteryStatus, s.IsWeak, s.FirstPracticedAt, s.LastPracticedAt);

    /// <summary>Tên ràng buộc do CHÍNH ta đặt trong <c>WritingAttemptConfiguration</c> — ổn định, không phụ thuộc kiểu Npgsql cụ thể (cùng kỹ thuật <c>QuizSubmissionService.IsClientAttemptIdUniqueViolation</c>).</summary>
    private static bool IsClientAttemptIdUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("ux_writing_attempts_client_attempt_id", StringComparison.OrdinalIgnoreCase) == true;
}
