namespace AntFarm.Chinese.Application.Common.Time;

/// <summary>
/// "Hôm nay" của một người học tại một thời điểm (R7-3, §5.2.8) — gói gọn
/// <c>access.users.time_zone</c> + đồng hồ (<see cref="TimeProvider"/>, đổi được trong test bằng
/// <c>FakeTimeProvider</c>) + <see cref="Domain.Time.UserLocalDate"/> để mọi dịch vụ SRS/F11 dùng
/// LẠI đúng một công thức cắt ngày, không tính tay rải rác.
/// </summary>
/// <param name="TimeZoneId">Múi giờ IANA đã chuẩn hoá của người dùng.</param>
/// <param name="NowUtc">"Bây giờ" — <c>Kind = Utc</c>.</param>
/// <param name="LocalDate">Ngày lịch "hôm nay" theo múi giờ người dùng.</param>
/// <param name="StartUtc">Mốc UTC bắt đầu <see cref="LocalDate"/> (00:00 giờ địa phương).</param>
/// <param name="EndUtc">Mốc UTC bắt đầu NGÀY KẾ TIẾP (nửa hở, R7-3 — dùng làm cận trên "đến hạn hôm nay").</param>
public sealed record UserDay(string TimeZoneId, DateTime NowUtc, DateOnly LocalDate, DateTime StartUtc, DateTime EndUtc);

public interface IUserDayContext
{
    /// <exception cref="AntFarm.Core.Errors.NotFoundException">Không tìm thấy user — lẽ ra <c>UserProvisioningMiddleware</c> đã tạo trước khi tới đây.</exception>
    Task<UserDay> GetAsync(Guid userId, CancellationToken ct);
}
