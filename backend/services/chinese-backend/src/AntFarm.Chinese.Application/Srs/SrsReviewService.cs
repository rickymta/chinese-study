using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Common.Time;
using AntFarm.Chinese.Application.Learning;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Srs;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Srs;

/// <summary>POST /api/srs/cards/{cardId}/reviews (§5.2.8, §6.2) — chấm một thẻ, idempotent theo <c>clientReviewId</c> (R7-8).</summary>
public sealed class SrsReviewService(
    IChineseDbContext db,
    IUserDayContext userDayContext,
    LearnerSettingsService learnerSettingsService,
    IStudyActivityRecorder studyActivityRecorder,
    SrsSummaryService summaryService)
{
    public async Task<ReviewCardResultDto> ReviewAsync(Guid userId, Guid cardId, ReviewCardCommand command, CancellationToken ct)
    {
        var existingLog = await FindExistingLogAsync(command.ClientReviewId, ct);
        if (existingLog is not null)
            return await BuildDuplicateResultAsync(userId, cardId, existingLog, ct);

        try
        {
            return await ReviewNewAsync(userId, cardId, command, ct);
        }
        catch (DbUpdateException ex) when (IsClientReviewIdUniqueViolation(ex))
        {
            // Đua hiếm (R7-8): hai request cùng clientReviewId gần như đồng thời — "await using"
            // của ReviewNewAsync đã tự ROLLBACK khi ném (transaction chưa Commit); người thua gỡ
            // ChangeTracker rồi đọc lại kết quả của người thắng (cùng kỹ thuật ToneDrillService).
            db.ClearTracking();

            var winner = await FindExistingLogAsync(command.ClientReviewId, ct)
                ?? throw new InvalidOperationException("Vi phạm duy nhất client_review_id nhưng không tìm thấy log tương ứng — không nên xảy ra.");

            return await BuildDuplicateResultAsync(userId, cardId, winner, ct);
        }
    }

    private async Task<ReviewCardResultDto> ReviewNewAsync(Guid userId, Guid cardId, ReviewCardCommand command, CancellationToken ct)
    {
        // FOR UPDATE chỉ khoá dòng hiệu quả TRONG một transaction đang mở (Postgres tự-commit từng
        // câu lệnh nếu không có transaction bao ngoài) — mở transaction TRƯỚC khi truy vấn khoá.
        await using var transaction = await db.BeginTransactionAsync(ct);

        var card = await db.SrsCards
            .FromSqlInterpolated($"SELECT * FROM learning.srs_cards WHERE id = {cardId} AND user_id = {userId} FOR UPDATE")
            .FirstOrDefaultAsync(ct);
        if (card is null)
            throw new NotFoundException($"Không tìm thấy thẻ '{cardId}'.");

        if (card.IsSuspended)
            throw new BusinessRuleException("CARD_SUSPENDED", "Thẻ đang tạm dừng.");

        var day = await userDayContext.GetAsync(userId, ct);
        var settings = await learnerSettingsService.GetEffectiveAsync(userId, ct);

        if (card.State == SrsState.New)
        {
            // R7-10 dưới đua: hai lượt chấm 2 THẺ MỚI KHÁC NHAU của CÙNG người gần như đồng thời
            // đều đọc newIntroducedToday TRƯỚC khi cái kia commit ⇒ có thể vượt daily_new_cards
            // đúng bằng số request đua nhau (FOR UPDATE ở trên chỉ khoá RIÊNG thẻ này, không ngăn
            // được đua giữa hai thẻ khác nhau). Khoá theo NGƯỜI DÙNG (advisory lock, tự nhả khi
            // transaction COMMIT/ROLLBACK — không cần unlock tay, không thêm bảng khoá riêng) để
            // serialize đúng đoạn "đếm rồi ghi" này; các đoạn khác của transaction không bị ảnh hưởng.
            await db.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtext({userId.ToString()})::bigint)", ct);

            var newIntroducedToday = await db.SrsCards.AsNoTracking()
                .CountAsync(c => c.UserId == userId && c.FirstReviewedLocalDate == day.LocalDate, ct);
            if (newIntroducedToday >= settings.DailyNewCards)
            {
                throw new BusinessRuleException(
                    "NEW_CARD_LIMIT_REACHED", "Đã đủ số từ mới hôm nay.", new { dailyNewCards = settings.DailyNewCards });
            }
        }

        var scheduler = new FsrsScheduler(FsrsOptions.Default((double)settings.DesiredRetention));
        var before = card.ToMemory();
        var schedulingResult = scheduler.Review(before, command.Rating, day.NowUtc);

        card.Apply(schedulingResult, command.Rating, day.NowUtc, day.LocalDate);

        var elapsedDays = before.LastReviewAt is null ? 0 : (day.NowUtc - before.LastReviewAt.Value).TotalDays;
        var log = SrsReviewLog.Create(
            command.ClientReviewId, card.Id, userId, command.Rating, day.NowUtc, day.LocalDate,
            before, schedulingResult.After, elapsedDays, schedulingResult.Interval.TotalDays, ClampDurationMs(command.DurationMs));
        db.SrsReviewLogs.Add(log);

        // R7-12: MỘT study_events cùng transaction — correct=1 trừ khi "Quên" (R7-1: rating<>again).
        await studyActivityRecorder.RecordAsync(
            userId, StudyEventKinds.SrsReview, day.NowUtc, 1, command.Rating != SrsRating.Again ? 1 : 0, card.Id, ct);

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var summary = await summaryService.GetAsync(userId, ct);
        return new ReviewCardResultDto(log.Id, Duplicate: false, SrsCardService.ToDto(card), summary);
    }

    private async Task<ReviewCardResultDto> BuildDuplicateResultAsync(Guid userId, Guid cardId, SrsReviewLog existingLog, CancellationToken ct)
    {
        if (existingLog.CardId != cardId || existingLog.UserId != userId)
            throw new ConflictException("CLIENT_REVIEW_ID_CONFLICT", "Mã đánh giá đã dùng cho thẻ khác.");

        var card = await db.SrsCards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == existingLog.CardId && c.UserId == userId, ct)
            ?? throw new NotFoundException($"Không tìm thấy thẻ '{existingLog.CardId}'.");

        var summary = await summaryService.GetAsync(userId, ct);
        return new ReviewCardResultDto(existingLog.Id, Duplicate: true, SrsCardService.ToDto(card), summary);
    }

    private Task<SrsReviewLog?> FindExistingLogAsync(Guid clientReviewId, CancellationToken ct) =>
        db.SrsReviewLogs.AsNoTracking().FirstOrDefaultAsync(l => l.ClientReviewId == clientReviewId, ct);

    private static int? ClampDurationMs(int? durationMs) => durationMs is null ? null : Math.Clamp(durationMs.Value, 0, 600_000);

    /// <summary>Tên ràng buộc do CHÍNH ta đặt trong <c>SrsReviewLogConfiguration</c> — ổn định, không phụ thuộc kiểu Npgsql cụ thể (cùng kỹ thuật <c>ToneDrillService.IsClientSessionIdUniqueViolation</c>).</summary>
    private static bool IsClientReviewIdUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("ux_srs_review_logs_client_review_id", StringComparison.OrdinalIgnoreCase) == true;
}
