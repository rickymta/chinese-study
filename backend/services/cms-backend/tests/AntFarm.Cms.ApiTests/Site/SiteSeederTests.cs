using AntFarm.Cms.ApiTests.Infrastructure;
using AntFarm.Cms.Application.Common.Options;
using AntFarm.Cms.Domain.Site;
using AntFarm.Cms.Infrastructure.Seeding;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AntFarm.Cms.ApiTests.Site;

/// <summary>
/// SiteSeeder idempotent (§5.2.3 W3a, test bắt buộc #6) — thao tác TRỰC TIẾP qua
/// <see cref="TestDbContextFactory"/> (chép khuôn AdminUsersTests.SeedAsync_ChayNhieuLan...).
/// </summary>
[Collection(CmsApiCollection.Name)]
public class SiteSeederTests(CmsDbApiFactory factory) : IClassFixture<CmsDbApiFactory>
{
    [DbFact]
    public async Task ChayHaiLan_KhongNhanDoiSettings()
    {
        await using var db = TestDbContextFactory.Create();
        var countBefore = await db.SiteSettings.CountAsync();

        await SiteSeeder.SeedAsync(db, new CmsSeedOptions(), TimeProvider.System, NullLogger.Instance, CancellationToken.None);
        await SiteSeeder.SeedAsync(db, new CmsSeedOptions(), TimeProvider.System, NullLogger.Instance, CancellationToken.None);

        var countAfter = await db.SiteSettings.CountAsync();
        countAfter.Should().Be(countBefore).And.Be(11);
    }

    [DbFact]
    public async Task XoaMotNgonNgu_RoiChaySeeder_KhongMocLai()
    {
        await using var db = TestDbContextFactory.Create();
        var throwaway = Language.Create(
            $"tam-{Guid.NewGuid():N}"[..12], "Tạm", "Tạm", "", "", LanguageStatus.Hidden, null, null,
            sortOrder: 999, actorId: null, DateTime.UtcNow);
        db.Languages.Add(throwaway);
        await db.SaveChangesAsync();

        var countAfterAdd = await db.Languages.CountAsync();
        db.Languages.Remove(throwaway);
        await db.SaveChangesAsync();

        await SiteSeeder.SeedAsync(db, new CmsSeedOptions(), TimeProvider.System, NullLogger.Instance, CancellationToken.None);

        var countAfterReseed = await db.Languages.CountAsync();
        countAfterReseed.Should().Be(countAfterAdd - 1); // bảng KHÔNG rỗng ⇒ seeder bỏ qua hoàn toàn, ngôn ngữ vừa xoá KHÔNG mọc lại
        (await db.Languages.AnyAsync(l => l.Id == throwaway.Id)).Should().BeFalse();
    }

    [DbFact]
    public async Task XoaMotDongSetting_RoiChaySeeder_ChenBuKhoaThieuGiuNguyenKhoaKhac()
    {
        await using var db = TestDbContextFactory.Create();

        // Khoá KHÁC (không bị xoá) đã sửa tay — phải GIỮ NGUYÊN sau khi seeder chạy.
        var edited = await db.SiteSettings.SingleAsync(s => s.Key == SiteSettingKeys.SocialTiktok);
        edited.Update("https://tiktok.com/@antfarm-test-giu-nguyen", Guid.NewGuid(), DateTime.UtcNow);
        await db.SaveChangesAsync();

        // Xoá HẲN dòng footer.text — mô phỏng dữ liệu bị xoá tay/lỗi.
        var toDelete = await db.SiteSettings.SingleAsync(s => s.Key == SiteSettingKeys.FooterText);
        db.SiteSettings.Remove(toDelete);
        await db.SaveChangesAsync();

        (await db.SiteSettings.CountAsync()).Should().Be(10);

        await SiteSeeder.SeedAsync(db, new CmsSeedOptions(), TimeProvider.System, NullLogger.Instance, CancellationToken.None);

        var footerText = await db.SiteSettings.SingleAsync(s => s.Key == SiteSettingKeys.FooterText);
        footerText.Value.Should().Be(""); // chèn bù ĐÚNG mặc định

        var tiktok = await db.SiteSettings.SingleAsync(s => s.Key == SiteSettingKeys.SocialTiktok);
        tiktok.Value.Should().Be("https://tiktok.com/@antfarm-test-giu-nguyen"); // khoá khác GIỮ NGUYÊN giá trị đã sửa

        (await db.SiteSettings.CountAsync()).Should().Be(11);
    }
}
