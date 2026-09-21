namespace AntFarm.Cms.Application.Common.Revalidation;

/// <summary>
/// Báo cho website (Next.js ISR) biết nội dung nào vừa đổi để làm mới cache (§5.4.3 — bảng tag,
/// thiết kế đầy đủ ở W7 <c>HttpRevalidationNotifier</c>). W3a chỉ có <c>NoopRevalidationNotifier</c>
/// (Infrastructure, log Debug) — website chưa tồn tại. Gọi SAU <c>SaveChangesAsync</c> thành công.
/// </summary>
public interface IRevalidationNotifier
{
    Task NotifyAsync(IReadOnlyCollection<string> tags, CancellationToken ct);
}
