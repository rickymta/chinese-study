using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Learning;
using AntFarm.Chinese.Application.Pinyin.Dtos;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Pinyin;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Pinyin;

/// <summary>POST /api/pinyin/tone-drills (§6.1, R5-10..R5-12) — nộp một phiên luyện thanh 20 câu, idempotent theo <c>clientSessionId</c>.</summary>
public sealed class ToneDrillService(
    IChineseDbContext db,
    IPinyinCatalog catalog,
    IStudyActivityRecorder studyActivityRecorder,
    TimeProvider timeProvider)
{
    public async Task<(SubmitToneDrillResponse Body, bool Created)> SubmitAsync(Guid userId, SubmitToneDrillRequest request, CancellationToken ct)
    {
        if (!catalog.IsAvailable)
            throw new ServiceUnavailableException("CONTENT_UNAVAILABLE", "Học liệu pinyin chưa sẵn sàng — báo quản trị viên.");

        var existing = await FindExistingAsync(userId, request.ClientSessionId, ct);
        if (existing is not null)
            return (await BuildResponseFromExistingAsync(existing, ct), false);

        ValidateItemsAgainstCatalog(request);
        ValidateSessionTime(request.StartedAt, request.FinishedAt);

        var domainItems = MapToDomainItems(request.Items);

        await using var transaction = await db.BeginTransactionAsync(ct);
        try
        {
            var session = ToneDrillSession.Create(
                userId, request.ClientSessionId, request.Mode, request.StartedAt, request.FinishedAt,
                domainItems, timeProvider.GetUtcNow().UtcDateTime);

            db.ToneDrillSessions.Add(session);
            foreach (var answer in session.Answers)
                db.ToneDrillAnswers.Add(answer);

            var studyEvent = await studyActivityRecorder.RecordAsync(
                userId, StudyEventKinds.ToneDrill, request.FinishedAt, session.Total, session.Correct, session.Id, ct);

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return (BuildResponse(session, studyEvent.LocalDate), true);
        }
        catch (DbUpdateException ex) when (IsClientSessionIdUniqueViolation(ex))
        {
            // Đua hiếm: hai request cùng nộp một clientSessionId gần như đồng thời (vd tab bị đơ,
            // người dùng bấm "Gửi lại" trong lúc request đầu vẫn đang xử lý) — người thua gỡ
            // ChangeTracker rồi đọc lại phiên người thắng vừa tạo (cùng kỹ thuật UserProvisioningService).
            await transaction.RollbackAsync(ct);
            db.ClearTracking();

            var winner = await FindExistingAsync(userId, request.ClientSessionId, ct);
            if (winner is null)
                throw;

            return (await BuildResponseFromExistingAsync(winner, ct), false);
        }
    }

    private Task<ToneDrillSession?> FindExistingAsync(Guid userId, Guid clientSessionId, CancellationToken ct) =>
        db.ToneDrillSessions.AsNoTracking()
            .Include(s => s.Answers)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ClientSessionId == clientSessionId, ct);

    private async Task<SubmitToneDrillResponse> BuildResponseFromExistingAsync(ToneDrillSession session, CancellationToken ct)
    {
        // local_date đã ghi vào study_events lúc nộp lần đầu (R-T3: không tính lại theo múi giờ
        // HIỆN TẠI của người dùng — có thể đã đổi múi giờ sau đó) — đọc lại đúng giá trị đã lưu.
        var localDate = await db.StudyEvents.AsNoTracking()
            .Where(e => e.RefId == session.Id && e.Kind == StudyEventKinds.ToneDrill)
            .Select(e => e.LocalDate)
            .FirstOrDefaultAsync(ct);

        return BuildResponse(session, localDate);
    }

    private static SubmitToneDrillResponse BuildResponse(ToneDrillSession session, DateOnly localDate) =>
        new(session.Id, session.ClientSessionId, session.Mode, session.Total, session.Correct, localDate, BuildByTone(session.Answers));

    private static IReadOnlyDictionary<string, ToneCountDto> BuildByTone(IReadOnlyList<ToneDrillAnswer> answers)
    {
        var byTone = new Dictionary<string, ToneCountDto>(4);
        for (var tone = 1; tone <= 4; tone++)
        {
            var total = 0;
            var correct = 0;
            foreach (var answer in answers)
            {
                if (answer.ExpectedTone != tone)
                    continue;
                total++;
                if (answer.IsCorrect)
                    correct++;
            }

            byTone[tone.ToString()] = new ToneCountDto(total, correct);
        }

        return byTone;
    }

    private void ValidateItemsAgainstCatalog(SubmitToneDrillRequest request)
    {
        for (var itemIndex = 0; itemIndex < request.Items.Count; itemIndex++)
        {
            var item = request.Items[itemIndex];
            for (var partIndex = 0; partIndex < item.Parts.Count; partIndex++)
            {
                var part = item.Parts[partIndex];

                if (!catalog.ContainsSyllable(part.Syllable))
                    throw new BusinessRuleException(
                        "UNKNOWN_SYLLABLE",
                        $"Âm tiết '{part.Syllable}' không có trong bảng pinyin.",
                        new { itemIndex, partIndex, syllable = part.Syllable });

                var hasExample = catalog.TryGetToneExample(part.Syllable, part.ExpectedTone, out var example);
                if (!hasExample || !string.Equals(example!.Hanzi, part.Hanzi, StringComparison.Ordinal))
                    throw new BusinessRuleException(
                        "TONE_NOT_AVAILABLE",
                        $"Âm tiết '{part.Syllable}' thanh {part.ExpectedTone} không có chữ minh hoạ.",
                        new { itemIndex, partIndex, syllable = part.Syllable, tone = part.ExpectedTone });
            }
        }
    }

    private void ValidateSessionTime(DateTime startedAt, DateTime finishedAt)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (startedAt > finishedAt)
            throw InvalidSessionTime("order");
        if (finishedAt > now.AddMinutes(5))
            throw InvalidSessionTime("future");
        if (finishedAt - startedAt > TimeSpan.FromHours(3))
            throw InvalidSessionTime("too_long");
        if (startedAt < now.AddHours(-24))
            throw InvalidSessionTime("too_old");
    }

    private static BusinessRuleException InvalidSessionTime(string reason) =>
        new("INVALID_SESSION_TIME", "Thời gian bài luyện không hợp lệ.", new { reason });

    private static IReadOnlyList<ToneDrillItemInput> MapToDomainItems(IReadOnlyList<SubmitToneDrillItemRequest> items)
    {
        var result = new List<ToneDrillItemInput>(items.Count);
        for (var itemIndex = 0; itemIndex < items.Count; itemIndex++)
        {
            var item = items[itemIndex];
            var parts = new List<ToneDrillPartInput>(item.Parts.Count);
            for (var partIndex = 0; partIndex < item.Parts.Count; partIndex++)
            {
                var part = item.Parts[partIndex];
                parts.Add(new ToneDrillPartInput(partIndex, part.Syllable, part.Hanzi, part.ExpectedTone, part.AnsweredTone));
            }

            result.Add(new ToneDrillItemInput(itemIndex, parts, item.ResponseMs, item.ReplayCount));
        }

        return result;
    }

    /// <summary>Tên ràng buộc do CHÍNH ta đặt trong <c>ToneDrillSessionConfiguration</c> — ổn định, không phụ thuộc kiểu Npgsql cụ thể (cùng kỹ thuật <c>UserProvisioningService.IsUsersPrimaryKeyViolation</c>).</summary>
    private static bool IsClientSessionIdUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("ux_tone_drill_sessions_user_id_client_session_id", StringComparison.OrdinalIgnoreCase) == true;
}
