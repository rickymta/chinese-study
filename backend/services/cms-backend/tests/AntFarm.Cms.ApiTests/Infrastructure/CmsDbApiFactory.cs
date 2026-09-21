using AntFarm.Testing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AntFarm.Cms.ApiTests.Infrastructure;

/// <summary>
/// Factory cho test [DbFact] — trỏ tới af_cms_test thật (schema di trú + seed danh mục MỘT LẦN
/// bởi <see cref="CmsDbFixture"/> trước khi bất kỳ test nào trong collection chạy).
///
/// Token do <see cref="TestTokenFactory"/> ký — KHÔNG cần identity-service chạy thật:
/// <c>PostConfigure&lt;JwtBearerOptions&gt;</c> thay <c>ConfigurationManager</c> (vốn tải JWKS qua
/// mạng) bằng <see cref="StaticConfigurationManager{T}"/> bọc khoá công khai cục bộ — PHẢI gán
/// <c>ConfigurationManager</c>, KHÔNG chỉ gán <c>Configuration</c> (bài học review F3
/// chinese-backend, xem doc-comment <c>TestTokenFactory</c>).
/// </summary>
public sealed class CmsDbApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "https://id.antfarms.xyz.test";
    public const string Audience = "af-cms";

    /// <summary>Email gán admin lúc provision lần đầu (R-W10) — dùng để test nhánh admin mà không cần thao tác DB trực tiếp.</summary>
    public const string BootstrapAdminEmail = "admin-bootstrap@vidu.com";

    public TestTokenFactory TokenFactory { get; } = new(Issuer);

    public CmsDbApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", TestDatabase.BuildConnectionString("af_cms_test"));
        Environment.SetEnvironmentVariable("AutoMigrate", "false"); // CmsDbFixture đã migrate + seed MỘT LẦN cho cả collection

        Environment.SetEnvironmentVariable("Auth__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Auth__Audience", Audience);
        // Không dùng thật — ConfigurationManager bị PostConfigure thay bằng khoá cục bộ ở dưới,
        // chỉ cần khác rỗng để AddAfJwtBearer(AfAuthOptions) không ném lúc khởi động.
        Environment.SetEnvironmentVariable("Auth__JwksUrl", "http://localhost:65535/.well-known/jwks.json");
        Environment.SetEnvironmentVariable("Auth__RequireHttpsMetadata", "false");

        // KHÔNG đặt CmsAccess__DefaultRoles__* — để trống, dùng đúng giá trị mặc định [] của
        // CmsAccessOptions (R-W2, fail-closed): người mới provision KHÔNG có vai trò nào.
        Environment.SetEnvironmentVariable("CmsAdmin__BootstrapEmails__0", BootstrapAdminEmail);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(TokenFactory.PublicKey);
                o.Configuration = configuration;
                o.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });
        });
    }
}
