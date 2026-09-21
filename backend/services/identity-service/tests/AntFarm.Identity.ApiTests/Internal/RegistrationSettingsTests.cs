using System.Net;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Internal;

/// <summary>D-W4, §5.2.9, §6.5 — bật/tắt đăng ký RUNTIME có hiệu lực NGAY (Invalidate cache).</summary>
[Collection(IdentityApiCollection.Name)]
public class RegistrationSettingsTests(IdentityInternalApiFactory factory) : IClassFixture<IdentityInternalApiFactory>
{
    private static readonly Guid AdminActorId = Guid.NewGuid();
    private const string AdminActorEmail = "admin@vidu.com";

    private static string NewEmail() => $"hocvien-{Guid.NewGuid():N}@vidu.com";

    private HttpClient CreateClient() => factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
    {
        HandleCookies = false,
        AllowAutoRedirect = false
    });

    private HttpRequestMessage InternalRequest(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Test-Local-Port", IdentityInternalApiFactory.InternalPort.ToString());
        request.Headers.Add("X-Service-Key", IdentityInternalApiFactory.ServiceKey);
        request.Headers.Add("X-Actor-Id", AdminActorId.ToString());
        request.Headers.Add("X-Actor-Email", AdminActorEmail);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonDefaults.Options);
        return request;
    }

    [DbFact]
    public async Task Get_TatBienDoiTruocKhiCoDong_TraVeSourceConfiguration()
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/internal/settings/registration");
        request.Headers.Add("X-Test-Local-Port", IdentityInternalApiFactory.InternalPort.ToString());
        request.Headers.Add("X-Service-Key", IdentityInternalApiFactory.ServiceKey);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RegistrationStateDto>(JsonDefaults.Options);
        body!.Source.Should().BeOneOf("configuration", "database"); // dòng dev khác đã PUT trước đó trong CÙNG collection tuần tự vẫn hợp lệ
    }

    [DbFact]
    public async Task TatDangKy_RegisterTraVe403NgaySauPUT()
    {
        var client = CreateClient();

        var putOff = await client.SendAsync(InternalRequest(HttpMethod.Put, "/internal/settings/registration", new { enabled = false }));
        putOff.StatusCode.Should().Be(HttpStatusCode.OK);
        var offBody = await putOff.Content.ReadFromJsonAsync<RegistrationStateDto>(JsonDefaults.Options);
        offBody!.Enabled.Should().BeFalse();
        offBody.Source.Should().Be("database");

        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await registerResponse.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("REGISTRATION_CLOSED");

        // Bật lại — không được để trạng thái "tắt" rò rỉ sang test khác chạy sau trong cùng collection.
        var putOn = await client.SendAsync(InternalRequest(HttpMethod.Put, "/internal/settings/registration", new { enabled = true }));
        putOn.StatusCode.Should().Be(HttpStatusCode.OK);

        var registerAfterOn = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);
        registerAfterOn.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private sealed record RegistrationStateDto(bool Enabled, string Source, DateTime? UpdatedAt);

    private sealed record ErrorDto(string Error, string Code);
}
