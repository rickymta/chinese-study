using AntFarm.Chinese.Domain.Time;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Time;

/// <summary>
/// Bổ sung cho <c>UserLocalDateTests</c> (F5) — §5.2.9: mốc "hôm nay" (R7-3) qua 00:00 giờ VN, và
/// <c>DayRange</c> quanh mốc chuyển giờ mùa hè (DST) của <c>America/New_York</c> (múi giờ có DST,
/// khác <c>Asia/Ho_Chi_Minh</c> không có DST).
/// </summary>
public class UserLocalDateSrsTests
{
    [Fact]
    public void From_1630Utc_AsiaHoChiMinh_LaNgay17_23Gio30()
    {
        var utc = new DateTime(2026, 9, 17, 16, 30, 0, DateTimeKind.Utc);

        UserLocalDate.From(utc, "Asia/Ho_Chi_Minh").Should().Be(new DateOnly(2026, 9, 17));
    }

    [Fact]
    public void From_1730Utc_AsiaHoChiMinh_LaNgay18_0Gio30()
    {
        var utc = new DateTime(2026, 9, 17, 17, 30, 0, DateTimeKind.Utc);

        UserLocalDate.From(utc, "Asia/Ho_Chi_Minh").Should().Be(new DateOnly(2026, 9, 18));
    }

    [Fact]
    public void DayRange_17Thang9_AsiaHoChiMinh()
    {
        var (fromUtc, toUtcExclusive) = UserLocalDate.DayRange(new DateOnly(2026, 9, 17), "Asia/Ho_Chi_Minh");

        fromUtc.Should().Be(new DateTime(2026, 9, 16, 17, 0, 0, DateTimeKind.Utc));
        toUtcExclusive.Should().Be(new DateTime(2026, 9, 17, 17, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>08/03/2026 (Chủ nhật): giờ mùa hè Mỹ bắt đầu lúc 2h sáng ĐỊA PHƯƠNG — nửa đêm (00:00, mốc DayRange dùng) vẫn còn EST (UTC-5), CHƯA nhảy giờ.</summary>
    [Fact]
    public void DayRange_08Thang3_AmericaNewYork_ConEst_UtcTru5()
    {
        var (fromUtc, _) = UserLocalDate.DayRange(new DateOnly(2026, 3, 8), "America/New_York");

        fromUtc.Should().Be(new DateTime(2026, 3, 8, 5, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>09/03/2026: đã qua mốc nhảy giờ (2h sáng 08/03) — nửa đêm ngày 09 đã là EDT (UTC-4).</summary>
    [Fact]
    public void DayRange_09Thang3_AmericaNewYork_DaLaEdt_UtcTru4()
    {
        var (fromUtc, _) = UserLocalDate.DayRange(new DateOnly(2026, 3, 9), "America/New_York");

        fromUtc.Should().Be(new DateTime(2026, 3, 9, 4, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>Cả hai mốc trả về LUÔN Kind=Utc dù múi giờ đầu vào có DST hay không (Npgsql timestamptz chỉ nhận Utc).</summary>
    [Fact]
    public void DayRange_AmericaNewYork_KindLuonUtc()
    {
        var (fromUtc, toUtcExclusive) = UserLocalDate.DayRange(new DateOnly(2026, 3, 8), "America/New_York");

        fromUtc.Kind.Should().Be(DateTimeKind.Utc);
        toUtcExclusive.Kind.Should().Be(DateTimeKind.Utc);
    }
}
