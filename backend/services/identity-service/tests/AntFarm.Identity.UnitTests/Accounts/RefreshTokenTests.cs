using AntFarm.Identity.Domain.Accounts;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.UnitTests.Accounts;

/// <summary>M1 (RM-A3) — RefreshToken.CreateNew ghi đúng kênh/clientApp/deviceName; AuthService dùng LẠI các trường này của token CHA khi xoay vòng (kế thừa kênh) thay vì tự suy từ request đang xoay.</summary>
public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateNew_KenhWeb_ClientAppVaDeviceNameNull()
    {
        var token = RefreshToken.CreateNew(
            Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), Now, refreshTokenDays: 30,
            RefreshClientType.Web, clientApp: null, deviceName: null,
            userAgent: "Mozilla/5.0", createdIp: "127.0.0.1");

        token.ClientType.Should().Be(RefreshClientType.Web);
        token.ClientApp.Should().BeNull();
        token.DeviceName.Should().BeNull();
        token.ExpiresAt.Should().Be(Now.AddDays(30));
    }

    [Fact]
    public void CreateNew_KenhMobile_GhiDungClientAppVaDeviceName()
    {
        var token = RefreshToken.CreateNew(
            Guid.NewGuid(), Guid.NewGuid(), new string('b', 64), Now, refreshTokenDays: 30,
            RefreshClientType.Mobile, clientApp: "chinese-mobile/1.0.0+1 (android)", deviceName: "Pixel 8",
            userAgent: null, createdIp: "1.2.3.4");

        token.ClientType.Should().Be(RefreshClientType.Mobile);
        token.ClientApp.Should().Be("chinese-mobile/1.0.0+1 (android)");
        token.DeviceName.Should().Be("Pixel 8");
    }

    /// <summary>Xoay vòng (mô phỏng đúng cách AuthService.RefreshAsync gọi lại CreateNew): token KẾ NHIỆM phải giữ NGUYÊN kênh/clientApp/deviceName của token CHA, chỉ UserAgent/CreatedIp đổi theo request xoay hiện tại.</summary>
    [Fact]
    public void XoayVong_TokenKeNhiem_KeThuaKenhClientAppDeviceNameCuaTokenCha()
    {
        var familyId = Guid.NewGuid();
        var parent = RefreshToken.CreateNew(
            Guid.NewGuid(), familyId, new string('c', 64), Now, refreshTokenDays: 30,
            RefreshClientType.Mobile, clientApp: "chinese-mobile/1.0.0+1 (ios)", deviceName: "iPhone 15",
            userAgent: null, createdIp: "10.0.0.1");

        var rotated = RefreshToken.CreateNew(
            parent.AccountId, parent.FamilyId, new string('d', 64), Now.AddMinutes(5), refreshTokenDays: 30,
            parent.ClientType, parent.ClientApp, parent.DeviceName,
            userAgent: "OKHTTP", createdIp: "10.0.0.2"); // UA/IP MỚI của request xoay — không kế thừa

        rotated.FamilyId.Should().Be(familyId);
        rotated.ClientType.Should().Be(RefreshClientType.Mobile);
        rotated.ClientApp.Should().Be("chinese-mobile/1.0.0+1 (ios)");
        rotated.DeviceName.Should().Be("iPhone 15");
        rotated.UserAgent.Should().Be("OKHTTP");
        rotated.CreatedIp.Should().Be("10.0.0.2");
    }

    [Fact]
    public void MarkRotated_DatRotatedAtVaReplacedById()
    {
        var token = RefreshToken.CreateNew(
            Guid.NewGuid(), Guid.NewGuid(), new string('e', 64), Now, refreshTokenDays: 30,
            RefreshClientType.Web, null, null, null, null);
        var replacedById = Guid.NewGuid();

        token.MarkRotated(Now, replacedById);

        token.RotatedAt.Should().Be(Now);
        token.ReplacedById.Should().Be(replacedById);
    }

    [Fact]
    public void Revoke_DatRevokedAtVaLyDo()
    {
        var token = RefreshToken.CreateNew(
            Guid.NewGuid(), Guid.NewGuid(), new string('f', 64), Now, refreshTokenDays: 30,
            RefreshClientType.Mobile, "chinese-mobile/1.0.0+1 (android)", null, null, null);

        token.Revoke(Now, "logout");

        token.RevokedAt.Should().Be(Now);
        token.RevokeReason.Should().Be("logout");
        token.IsActive(Now).Should().BeFalse();
    }
}
