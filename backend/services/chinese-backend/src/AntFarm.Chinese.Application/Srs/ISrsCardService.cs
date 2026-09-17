namespace AntFarm.Chinese.Application.Srs;

/// <summary>
/// Tạo/tạm dừng thẻ SRS ngoài luồng chấm thẻ (§5.2.8). <see cref="EnsureCardsAsync"/> là điểm khớp
/// K12 với F9 (§4.2 hợp đồng F8-F11): hoàn thành bài học lần đầu gọi lại ĐÚNG hàm này (không tự
/// viết lại logic tạo thẻ ở F9) để thẻ <c>source='lesson'</c> tuân R-LS5/R-LS6 giống hệt thẻ
/// <c>source='path'</c> do <see cref="SrsQueueService"/> tạo lười.
/// </summary>
public interface ISrsCardService
{
    /// <summary>
    /// Tạo thẻ <c>hanzi_to_meaning</c>/<c>state=new</c> cho các <paramref name="wordIds"/> CHƯA có
    /// thẻ của <paramref name="userId"/>. Ghi NGAY qua SQL trực tiếp (<c>INSERT ... ON CONFLICT DO
    /// NOTHING</c>, KHÔNG qua ChangeTracker) — KHÔNG cần (và không nên) gọi thêm
    /// <c>SaveChangesAsync</c> cho các dòng này; nếu người gọi đang ở giữa transaction của họ (F9:
    /// thẻ + study_events + lesson_progress, K12) thì lệnh ghi này TỰ THAM GIA transaction đó (cùng
    /// DbContext) và <c>SaveChangesAsync</c> của họ vẫn cần cho các thay đổi KHÁC như bình thường.
    /// Hai lượt gọi đua nhau tạo CÙNG một thẻ (2 tab, hoặc hàng đợi đua với F9 hoàn thành bài học)
    /// tự nhường nhau, không ném <c>23505</c>. Từ đã có thẻ (bất kỳ nguồn nào) bị BỎ QUA, không đổi
    /// <c>source</c>/trạng thái đã có. Trả số thẻ MỚI được thêm.
    /// </summary>
    Task<int> EnsureCardsAsync(Guid userId, IReadOnlyList<Guid> wordIds, string source, CancellationToken ct);

    /// <summary>POST /api/srs/cards (§6.2) — người học TỰ thêm từ (nguồn <c>manual</c>); 422 <c>UNKNOWN_WORD</c> nếu có wordId không tồn tại (không thêm thẻ nào).</summary>
    Task<AddCardsResultDto> AddAsync(Guid userId, AddCardsCommand command, CancellationToken ct);

    /// <summary>PUT /api/srs/cards/{cardId}/suspension (§6.2) — 404 nếu không phải thẻ của chính người gọi.</summary>
    Task<SrsCardDto> SetSuspendedAsync(Guid userId, Guid cardId, bool suspended, CancellationToken ct);
}
