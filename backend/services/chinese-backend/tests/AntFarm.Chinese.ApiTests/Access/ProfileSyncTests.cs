using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Access;

/// <summary>
/// F4/RK10/R4-4 — <c>UserProvisioningService</c> phải đồng bộ hồ sơ NGAY khi claim (name/email/
/// zoneinfo) khác bản ghi đã lưu, KHÔNG chờ hết cửa sổ cache 5 phút (bug F3: cache theo mốc thời
/// gian đơn thuần bỏ qua HOÀN TOÀN mọi thay đổi trong 5 phút). Không cần advance TimeProvider —
/// chính vì test PHẢI xanh dù KHÔNG qua 5 phút mới chứng minh được sửa lỗi.
/// </summary>
[Collection(ChineseApiCollection.Name)]
public class ProfileSyncTests(ChineseDbApiFactory factory) : IClassFixture<ChineseDbApiFactory>
{
    private HttpClient ClientFor(Guid accountId, string email, string displayName, string timeZone)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.TokenFactory.CreateToken(accountId, email: email, displayName: displayName, timeZone: timeZone));
        return client;
    }

    [DbFact]
    public async Task DoiMuiGioNgay_KhongCho5Phut_MeTraVeMuiGioMoi()
    {
        var accountId = Guid.NewGuid();
        var email = $"sync-tz-{Guid.NewGuid():N}@vidu.com";

        var first = await ClientFor(accountId, email, "Học viên", "Asia/Ho_Chi_Minh").GetAsync("/api/me");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        (await first.Content.ReadFromJsonAsync<MeDto>(JsonDefaults.Options))!.TimeZone.Should().Be("Asia/Ho_Chi_Minh");

        // NGAY LẬP TỨC (đồng hồ giả KHÔNG advance) — claim zoneinfo khác ⇒ phải đồng bộ NGAY.
        var second = await ClientFor(accountId, email, "Học viên", "Europe/Berlin").GetAsync("/api/me");
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await second.Content.ReadFromJsonAsync<MeDto>(JsonDefaults.Options);
        body!.TimeZone.Should().Be("Europe/Berlin");
    }

    [DbFact]
    public async Task DoiTenHienThiNgay_KhongCho5Phut_MeTraVeTenMoi()
    {
        var accountId = Guid.NewGuid();
        var email = $"sync-name-{Guid.NewGuid():N}@vidu.com";

        (await ClientFor(accountId, email, "Ten Cu", "Asia/Ho_Chi_Minh").GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await ClientFor(accountId, email, "Ten Moi", "Asia/Ho_Chi_Minh").GetAsync("/api/me");
        var body = await second.Content.ReadFromJsonAsync<MeDto>(JsonDefaults.Options);
        body!.DisplayName.Should().Be("Ten Moi");
    }

    [DbFact]
    public async Task ZoneinfoRong_GiuMuiGioCu_KhongChanRequest()
    {
        var accountId = Guid.NewGuid();
        var email = $"sync-tzempty-{Guid.NewGuid():N}@vidu.com";

        (await ClientFor(accountId, email, "Học viên", "Asia/Ho_Chi_Minh").GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        // zoneinfo rỗng (không truyền timeZone khác) — dùng token KHÔNG có claim zoneinfo hợp lệ
        // bằng cách gọi trực tiếp với chuỗi rỗng qua CreateToken (timeZone: "") mô phỏng claim rỗng.
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.TokenFactory.CreateToken(accountId, email: email, displayName: "Học viên", timeZone: ""));

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MeDto>(JsonDefaults.Options);
        body!.TimeZone.Should().Be("Asia/Ho_Chi_Minh"); // giữ nguyên múi giờ cũ, không ép về mặc định/chặn request
    }

    private sealed record MeDto(Guid Id, string Email, string DisplayName, string TimeZone, List<string> Roles, List<string> Permissions, DateTime FirstSeenAt);
}
