using AntFarm.Identity.Domain.Accounts;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.UnitTests.Accounts;

/// <summary>R-A8 — khoá tài khoản sau N lần đăng nhập sai liên tiếp.</summary>
public class AccountTests
{
    private static readonly DateTime Now = new(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RegisterFailedLogin_ChuaDatNguong_ChuaKhoa()
    {
        var account = Account.Register("a@vidu.com", "A", "hash", "Asia/Ho_Chi_Minh", Now);

        for (var i = 0; i < 9; i++)
            account.RegisterFailedLogin(Now, maxFailedLogins: 10, lockoutMinutes: 15);

        account.IsLockedOut(Now).Should().BeFalse();
        account.FailedLoginCount.Should().Be(9);
    }

    [Fact]
    public void RegisterFailedLogin_DatNguong_KhoaVaDemVeKhong()
    {
        var account = Account.Register("a@vidu.com", "A", "hash", "Asia/Ho_Chi_Minh", Now);

        for (var i = 0; i < 10; i++)
            account.RegisterFailedLogin(Now, maxFailedLogins: 10, lockoutMinutes: 15);

        account.IsLockedOut(Now).Should().BeTrue();
        account.LockoutUntil.Should().Be(Now.AddMinutes(15));
        account.FailedLoginCount.Should().Be(0); // đếm lại từ 0 sau khi khoá
    }

    [Fact]
    public void IsLockedOut_SauKhiHetHanKhoa_TraVeFalse()
    {
        var account = Account.Register("a@vidu.com", "A", "hash", "Asia/Ho_Chi_Minh", Now);
        for (var i = 0; i < 10; i++)
            account.RegisterFailedLogin(Now, maxFailedLogins: 10, lockoutMinutes: 15);

        account.IsLockedOut(Now.AddMinutes(16)).Should().BeFalse();
    }

    [Fact]
    public void RegisterSuccessfulLogin_DatLaiDemVaXoaKhoa()
    {
        var account = Account.Register("a@vidu.com", "A", "hash", "Asia/Ho_Chi_Minh", Now);
        account.RegisterFailedLogin(Now, maxFailedLogins: 10, lockoutMinutes: 15);
        account.RegisterFailedLogin(Now, maxFailedLogins: 10, lockoutMinutes: 15);

        account.RegisterSuccessfulLogin(Now);

        account.FailedLoginCount.Should().Be(0);
        account.LockoutUntil.Should().BeNull();
        account.LastLoginAt.Should().Be(Now);
    }

    [Fact]
    public void NormalizeEmail_BoKhoangTrangVaVeThuong()
    {
        Account.NormalizeEmail("  Ban@VIDU.com  ").Should().Be("ban@vidu.com");
    }
}
