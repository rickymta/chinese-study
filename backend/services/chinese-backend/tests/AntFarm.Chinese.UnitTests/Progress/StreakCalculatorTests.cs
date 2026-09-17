using AntFarm.Chinese.Domain.Progress;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Progress;

/// <summary>F11, §5.2.4 — hàm THUẦN, không đụng DB (cùng kỹ thuật <c>ToneStatsCalculatorTests</c>, F5).</summary>
public class StreakCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 9, 17);

    [Fact]
    public void Calculate_RongKhongCoNgayNao_TraVeKhongKhongFalse()
    {
        var result = StreakCalculator.Calculate([], Today);

        result.Current.Should().Be(0);
        result.Longest.Should().Be(0);
        result.StudiedToday.Should().BeFalse();
    }

    [Fact]
    public void Calculate_ChiHomNay_HienTaiVaDaiNhatBang1_StudiedTodayTrue()
    {
        var result = StreakCalculator.Calculate([Today], Today);

        result.Current.Should().Be(1);
        result.Longest.Should().Be(1);
        result.StudiedToday.Should().BeTrue();
    }

    [Fact]
    public void Calculate_ChiHomQua_HienTaiVaDaiNhatBang1_StudiedTodayFalse()
    {
        var result = StreakCalculator.Calculate([Today.AddDays(-1)], Today);

        result.Current.Should().Be(1);
        result.Longest.Should().Be(1);
        result.StudiedToday.Should().BeFalse();
    }

    [Fact]
    public void Calculate_ChiHomKia_HienTaiBang0_DaiNhatBang1()
    {
        var result = StreakCalculator.Calculate([Today.AddDays(-2)], Today);

        result.Current.Should().Be(0);
        result.Longest.Should().Be(1);
        result.StudiedToday.Should().BeFalse();
    }

    [Fact]
    public void Calculate_BaNgayLienTiepToiHomQuaCongHomNay_HienTaiBang4()
    {
        DateOnly[] dates = [Today.AddDays(-3), Today.AddDays(-2), Today.AddDays(-1), Today];

        var result = StreakCalculator.Calculate(dates, Today);

        result.Current.Should().Be(4);
        result.Longest.Should().Be(4);
        result.StudiedToday.Should().BeTrue();
    }

    [Fact]
    public void Calculate_ChuoiCu10NgayVaChuoiHienTai2Ngay_TachBietDung()
    {
        // Chuỗi cũ 10 ngày (đã đứt quãng) rồi một chuỗi mới 2 ngày tới hôm nay.
        var oldRun = Enumerable.Range(0, 10).Select(i => Today.AddDays(-30 - i));
        DateOnly[] recentRun = [Today.AddDays(-1), Today];
        var dates = oldRun.Concat(recentRun).ToArray();

        var result = StreakCalculator.Calculate(dates, Today);

        result.Current.Should().Be(2);
        result.Longest.Should().Be(10);
        result.StudiedToday.Should().BeTrue();
    }

    [Fact]
    public void Calculate_NgayTrungLapTrongInput_KhongDemDoi()
    {
        DateOnly[] dates = [Today, Today, Today.AddDays(-1), Today.AddDays(-1)];

        var result = StreakCalculator.Calculate(dates, Today);

        result.Current.Should().Be(2);
        result.Longest.Should().Be(2);
    }

    [Fact]
    public void Calculate_NgayTuongLai_BiBoQuaChoHienTai_NhungVanTinhVaoDaiNhat()
    {
        // Đổi múi giờ có thể sinh một ngày > today trong dữ liệu lịch sử — không được đếm vào
        // "hiện tại" (R-PG3) nhưng vẫn hợp lệ để tính "dài nhất" (đoạn liên tiếp riêng, không nối
        // với hôm nay vì có khoảng trống).
        DateOnly[] dates = [Today.AddDays(3), Today.AddDays(4)];

        var result = StreakCalculator.Calculate(dates, Today);

        result.Current.Should().Be(0);
        result.Longest.Should().Be(2);
        result.StudiedToday.Should().BeFalse();
    }

    [Fact]
    public void Calculate_QuaRanhGioiNam_31Thang12SangMung1Thang1_DemLienTiep()
    {
        var today = new DateOnly(2027, 1, 1);
        DateOnly[] dates = [new DateOnly(2026, 12, 31), today];

        var result = StreakCalculator.Calculate(dates, today);

        result.Current.Should().Be(2);
        result.Longest.Should().Be(2);
    }

    [Fact]
    public void Calculate_NamNhuan_28Thang2Sang29Thang2Sang1Thang3_DemLienTiep()
    {
        var today = new DateOnly(2028, 3, 1); // 2028 là năm nhuận
        DateOnly[] dates = [new DateOnly(2028, 2, 28), new DateOnly(2028, 2, 29), today];

        var result = StreakCalculator.Calculate(dates, today);

        result.Current.Should().Be(3);
        result.Longest.Should().Be(3);
    }

    [Fact]
    public void Calculate_BoNgayHomQua_HienTaiBang0KhiHomNayChuaHoc()
    {
        // Ba ngày trước liên tiếp NHƯNG bỏ hôm qua và hôm nay ⇒ chuỗi hiện tại đứt hoàn toàn.
        DateOnly[] dates = [Today.AddDays(-4), Today.AddDays(-3), Today.AddDays(-2)];

        var result = StreakCalculator.Calculate(dates, Today);

        result.Current.Should().Be(0);
        result.Longest.Should().Be(3);
        result.StudiedToday.Should().BeFalse();
    }

    [Fact]
    public void Calculate_ThuTuNgayTrongInputXaoTron_KhongAnhHuongKetQua()
    {
        DateOnly[] dates = [Today, Today.AddDays(-2), Today.AddDays(-1)];

        var result = StreakCalculator.Calculate(dates, Today);

        result.Current.Should().Be(3);
        result.Longest.Should().Be(3);
    }
}
