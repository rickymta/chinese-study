using AntFarm.Core.Errors;
using AntFarm.Identity.Application.Admin;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.UnitTests.Admin;

/// <summary>§5.2.9 — ranh giới ngày Việt Nam của thống kê đăng ký (tài khoản tạo 23:30 VN ngày D vs 00:30 VN ngày D+1).</summary>
public class RegistrationStatsCalculatorTests
{
    [Fact]
    public void ToVietnamDate_2330GioVietNam_ThuocNgayD()
    {
        // 23:30 giờ VN ngày 2026-09-17 = 16:30 UTC cùng ngày (VN = UTC+7).
        var utc = new DateTime(2026, 9, 17, 16, 30, 0, DateTimeKind.Utc);

        RegistrationStatsCalculator.ToVietnamDate(utc).Should().Be(new DateOnly(2026, 9, 17));
    }

    [Fact]
    public void ToVietnamDate_0030GioVietNamNgayKeTiep_ThuocNgayD1()
    {
        // 00:30 giờ VN ngày 2026-09-18 = 17:30 UTC ngày 2026-09-17.
        var utc = new DateTime(2026, 9, 17, 17, 30, 0, DateTimeKind.Utc);

        RegistrationStatsCalculator.ToVietnamDate(utc).Should().Be(new DateOnly(2026, 9, 18));
    }

    [Fact]
    public void ToVietnamDate_HaiMocGanNhauQuaNuaDem_RoiVaoHaiNgayKhacNhau()
    {
        var truocNuaDem = new DateTime(2026, 9, 17, 16, 30, 0, DateTimeKind.Utc);
        var sauNuaDem = new DateTime(2026, 9, 17, 17, 30, 0, DateTimeKind.Utc);

        RegistrationStatsCalculator.ToVietnamDate(truocNuaDem)
            .Should().NotBe(RegistrationStatsCalculator.ToVietnamDate(sauNuaDem));
    }

    [Fact]
    public void BuildDays_NgayKhongCoDangKy_TraVeCount0()
    {
        var from = new DateOnly(2026, 9, 15);
        var to = new DateOnly(2026, 9, 17);
        var counts = new Dictionary<DateOnly, int> { [new DateOnly(2026, 9, 16)] = 3 };

        var days = RegistrationStatsCalculator.BuildDays(from, to, counts);

        days.Should().HaveCount(3);
        days[0].Should().Be(new RegistrationDayDto(new DateOnly(2026, 9, 15), 0));
        days[1].Should().Be(new RegistrationDayDto(new DateOnly(2026, 9, 16), 3));
        days[2].Should().Be(new RegistrationDayDto(new DateOnly(2026, 9, 17), 0));
    }

    [Fact]
    public void EnsureValidRange_FromLonHonTo_NemValidationAppException()
    {
        var act = () => RegistrationStatsCalculator.EnsureValidRange(new DateOnly(2026, 9, 20), new DateOnly(2026, 9, 10));

        act.Should().Throw<ValidationAppException>();
    }

    [Fact]
    public void EnsureValidRange_367Ngay_NemValidationAppException()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = from.AddDays(366); // 367 ngày (từ + đến đều tính, DayNumber chênh 366)

        var act = () => RegistrationStatsCalculator.EnsureValidRange(from, to);

        act.Should().Throw<ValidationAppException>();
    }

    [Fact]
    public void EnsureValidRange_Dung366Ngay_KhongNem()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = from.AddDays(365); // đúng 366 ngày

        var act = () => RegistrationStatsCalculator.EnsureValidRange(from, to);

        act.Should().NotThrow();
    }

    [Fact]
    public void ResolveRange_KhongTruyenGi_MacDinh30NgayKetThucHomNay()
    {
        var today = new DateOnly(2026, 9, 17);

        var (from, to) = RegistrationStatsCalculator.ResolveRange(null, null, today);

        to.Should().Be(today);
        from.Should().Be(today.AddDays(-29));
    }
}
