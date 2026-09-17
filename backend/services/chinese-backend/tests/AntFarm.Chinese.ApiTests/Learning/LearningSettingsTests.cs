using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Xml;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Chinese.Domain.Srs;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Learning;

/// <summary>GET/PUT /api/me/learning-settings (§6.2, R7-14).</summary>
[Collection(ChineseApiCollection.Name)]
public class LearningSettingsTests
{
    public LearningSettingsTests() => Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());

    [DbFact]
    public async Task Get_ChuaTungLuu_TraMacDinh_IsDefaultTrue()
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());

        var response = await client.GetAsync("/api/me/learning-settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SettingsDto>(JsonDefaults.Options);
        body!.IsDefault.Should().BeTrue();
        body.DailyNewCards.Should().Be(10);
        body.DailyReviewLimit.Should().Be(200);
        body.DesiredRetention.Should().Be(0.90m);
    }

    [DbTheory]
    [InlineData(0.99)]
    [InlineData(0.79)]
    public async Task Put_DesiredRetentionNgoaiKhoang_Tra400(double desiredRetention)
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());

        var response = await client.PutAsJsonAsync(
            "/api/me/learning-settings",
            new { dailyNewCards = 10, dailyReviewLimit = 200, desiredRetention, ttsRate = 0.80, autoPlayAudio = true },
            JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("VALIDATION");
    }

    [DbFact]
    public async Task Put_DesiredRetention085_HopLe_LuotChamSauDungRMoiDeLapLich()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        var now = new DateTimeOffset(2026, 9, 17, 1, 0, 0, TimeSpan.Zero);
        factory.TimeProvider.AdjustTime(now);

        var putResponse = await client.PutAsJsonAsync(
            "/api/me/learning-settings",
            new { dailyNewCards = 10, dailyReviewLimit = 200, desiredRetention = 0.85, ttsRate = 0.80, autoPlayAudio = true },
            JsonDefaults.Options);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var putBody = await putResponse.Content.ReadFromJsonAsync<SettingsDto>(JsonDefaults.Options);
        putBody!.IsDefault.Should().BeFalse();
        putBody.DesiredRetention.Should().Be(0.85m);

        var getResponse = await client.GetAsync("/api/me/learning-settings");
        var getBody = await getResponse.Content.ReadFromJsonAsync<SettingsDto>(JsonDefaults.Options);
        getBody!.DesiredRetention.Should().Be(0.85m);

        // Rating "easy" từ thẻ MỚI ⇒ chuyển thẳng Review, khoảng = I(S=8.2956) — PHỤ THUỘC
        // desired_retention (khác "good"/"hard" ở bước học vẫn cố định theo phút, không đổi theo r).
        // So khớp với TÍNH TAY bằng chính FsrsScheduler(r=0,85) — xác nhận API DÙNG THẬT cài đặt vừa
        // lưu (R7-14), không lẫn mặc định 0,90 (V7 hợp đồng: S=10, r=0,85 ⇒ 19 ngày, r=0,90 ⇒ 10 ngày — khác hẳn nhau).
        var queue = await GetQueueAsync(client);
        var easyInterval = XmlConvert.ToTimeSpan(queue.Cards[0].Intervals["easy"]);

        var expectedScheduler = new FsrsScheduler(FsrsOptions.Default(0.85));
        var newCardMemory = new SrsMemory(SrsState.New, null, null, null, now.UtcDateTime, null);
        var expectedInterval = expectedScheduler.Review(newCardMemory, SrsRating.Easy, now.UtcDateTime).Interval;

        easyInterval.Should().Be(expectedInterval);

        var defaultScheduler = new FsrsScheduler(FsrsOptions.Default(0.90));
        var defaultInterval = defaultScheduler.Review(newCardMemory, SrsRating.Easy, now.UtcDateTime).Interval;
        easyInterval.Should().NotBe(defaultInterval); // r=0,85 phải cho khoảng KHÁC r=0,90 mặc định.
    }

    private async Task<QueueResultDto> GetQueueAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/srs/queue");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<QueueResultDto>(JsonDefaults.Options))!;
    }

    private static HttpClient ClientFor(ChineseDbApiFactory factory, Guid userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenFactory.CreateToken(userId));
        return client;
    }

    private static string ResolveRealContentRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "content", "chinese");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return "content/chinese";
    }

    private sealed record SettingsDto(short DailyNewCards, short DailyReviewLimit, decimal DesiredRetention, decimal TtsRate, bool AutoPlayAudio, bool IsDefault);
    private sealed record ErrorDto(string Error, string Code);
    private sealed record QueueCardDto(Dictionary<string, string> Intervals);
    private sealed record QueueResultDto(List<QueueCardDto> Cards);
}
