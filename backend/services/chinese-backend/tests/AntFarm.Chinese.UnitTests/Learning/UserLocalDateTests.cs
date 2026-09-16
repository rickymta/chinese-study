using AntFarm.Chinese.Domain.Time;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Learning;

public class UserLocalDateTests
{
    [Fact]
    public void From_2330Utc_AsiaHoChiMinh_LaSangHomSau()
    {
        var utc = new DateTime(2026, 9, 16, 23, 30, 0, DateTimeKind.Utc);

        UserLocalDate.From(utc, "Asia/Ho_Chi_Minh").Should().Be(new DateOnly(2026, 9, 17));
    }

    [Fact]
    public void From_1659Utc_AsiaHoChiMinh_ChuaSangNgayMoi()
    {
        // 16:59Z + 7h = 23:59 giờ VN cùng ngày.
        var utc = new DateTime(2026, 9, 16, 16, 59, 0, DateTimeKind.Utc);

        UserLocalDate.From(utc, "Asia/Ho_Chi_Minh").Should().Be(new DateOnly(2026, 9, 16));
    }

    [Fact]
    public void From_1700Utc_AsiaHoChiMinh_VuaSangNgayMoi()
    {
        // 17:00Z + 7h = 00:00 giờ VN — mốc chuyển ngày chính xác.
        var utc = new DateTime(2026, 9, 16, 17, 0, 0, DateTimeKind.Utc);

        UserLocalDate.From(utc, "Asia/Ho_Chi_Minh").Should().Be(new DateOnly(2026, 9, 17));
    }

    [Fact]
    public void From_TimeZoneRac_DungMacDinhKhongNem()
    {
        var utc = new DateTime(2026, 9, 16, 23, 30, 0, DateTimeKind.Utc);

        var act = () => UserLocalDate.From(utc, "Khong/Ton_Tai");

        act.Should().NotThrow();
        UserLocalDate.From(utc, "Khong/Ton_Tai").Should().Be(UserLocalDate.From(utc, UserLocalDate.DefaultTimeZoneId));
    }

    [Fact]
    public void From_TimeZoneRong_DungMacDinhKhongNem()
    {
        var utc = new DateTime(2026, 9, 16, 23, 30, 0, DateTimeKind.Utc);

        var act = () => UserLocalDate.From(utc, "");

        act.Should().NotThrow();
    }

    [Fact]
    public void DayRange_TraVeKindUtcVaNuaHo()
    {
        var date = new DateOnly(2026, 9, 17);

        var (fromUtc, toUtcExclusive) = UserLocalDate.DayRange(date, "Asia/Ho_Chi_Minh");

        fromUtc.Kind.Should().Be(DateTimeKind.Utc);
        toUtcExclusive.Kind.Should().Be(DateTimeKind.Utc);
        fromUtc.Should().Be(new DateTime(2026, 9, 16, 17, 0, 0, DateTimeKind.Utc));
        toUtcExclusive.Should().Be(new DateTime(2026, 9, 17, 17, 0, 0, DateTimeKind.Utc));

        // Nửa hở: mốc chính xác 17:00Z (00:00 giờ VN ngày 18) thuộc ngày KẾ TIẾP, không thuộc ngày này.
        UserLocalDate.From(toUtcExclusive, "Asia/Ho_Chi_Minh").Should().Be(date.AddDays(1));
        UserLocalDate.From(toUtcExclusive.AddTicks(-1), "Asia/Ho_Chi_Minh").Should().Be(date);
    }
}
