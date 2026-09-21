using AntFarm.Identity.Api.Internal;
using AntFarm.Identity.Application.Common.Options;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.UnitTests.Internal;

/// <summary>R-W4 (§5.2.9) — ma trận đầy đủ của <see cref="InternalAccessPolicy"/>.</summary>
public class InternalAccessPolicyTests
{
    private const int InternalPort = 5291;
    private const int PublicPort = 5281;
    private const string ValidKey = "dev-internal-key-change-me-0123456789abcdef"; // >= 32 ký tự

    private static readonly InternalOptions Enabled = new() { Port = InternalPort, ServiceKey = ValidKey };
    private static readonly InternalOptions DisabledByPort = new() { Port = 0, ServiceKey = ValidKey };
    private static readonly InternalOptions DisabledByShortKey = new() { Port = InternalPort, ServiceKey = "qua-ngan" };

    [Fact]
    public void Tat_PortBang0_RouteNoiBo_TraVe404()
    {
        InternalAccessPolicy.Evaluate(isInternalPath: true, localPort: InternalPort, DisabledByPort, ValidKey)
            .Should().Be(InternalAccessDecision.NotFound);
    }

    [Fact]
    public void Tat_KhoaQuaNgan_RouteNoiBo_TraVe404()
    {
        InternalAccessPolicy.Evaluate(isInternalPath: true, localPort: InternalPort, DisabledByShortKey, ValidKey)
            .Should().Be(InternalAccessDecision.NotFound);
    }

    [Fact]
    public void Tat_RouteThuong_DiTiepBinhThuong()
    {
        InternalAccessPolicy.Evaluate(isInternalPath: false, localPort: PublicPort, DisabledByPort, providedKey: null)
            .Should().Be(InternalAccessDecision.PassThrough);
    }

    [Fact]
    public void Bat_CongCongKhai_RouteNoiBo_TraVe404_KeCaKhoaDung()
    {
        InternalAccessPolicy.Evaluate(isInternalPath: true, localPort: PublicPort, Enabled, ValidKey)
            .Should().Be(InternalAccessDecision.NotFound);
    }

    [Fact]
    public void Bat_CongNoiBo_RouteThuong_TraVe404()
    {
        InternalAccessPolicy.Evaluate(isInternalPath: false, localPort: InternalPort, Enabled, ValidKey)
            .Should().Be(InternalAccessDecision.NotFound);
    }

    [Fact]
    public void Bat_CongNoiBo_ThieuKhoa_TraVe401()
    {
        InternalAccessPolicy.Evaluate(isInternalPath: true, localPort: InternalPort, Enabled, providedKey: null)
            .Should().Be(InternalAccessDecision.Unauthorized);
    }

    [Fact]
    public void Bat_CongNoiBo_KhoaSai_TraVe401()
    {
        InternalAccessPolicy.Evaluate(isInternalPath: true, localPort: InternalPort, Enabled, providedKey: "khoa-sai-hoan-toan-nhung-du-dai-32-ky-tu")
            .Should().Be(InternalAccessDecision.Unauthorized);
    }

    [Fact]
    public void Bat_CongNoiBo_KhoaKhacDoDai_TraVe401()
    {
        InternalAccessPolicy.Evaluate(isInternalPath: true, localPort: InternalPort, Enabled, providedKey: "ngan")
            .Should().Be(InternalAccessDecision.Unauthorized);
    }

    [Fact]
    public void Bat_CongNoiBo_KhoaDung_TraVeAllow()
    {
        InternalAccessPolicy.Evaluate(isInternalPath: true, localPort: InternalPort, Enabled, ValidKey)
            .Should().Be(InternalAccessDecision.Allow);
    }

    [Fact]
    public void Bat_CongCongKhai_RouteThuong_DiTiepBinhThuong()
    {
        InternalAccessPolicy.Evaluate(isInternalPath: false, localPort: PublicPort, Enabled, providedKey: null)
            .Should().Be(InternalAccessDecision.PassThrough);
    }
}
