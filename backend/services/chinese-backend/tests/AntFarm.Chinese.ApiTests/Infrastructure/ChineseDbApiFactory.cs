using AntFarm.Testing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AntFarm.Chinese.ApiTests.Infrastructure;

/// <summary>
/// Factory cho test [DbFact] — trỏ tới af_chinese_test thật (§9.2, schema di trú + seed danh mục
/// MỘT LẦN bởi <see cref="ChineseDbFixture"/> trước khi bất kỳ test nào trong collection chạy).
///
/// Token do <see cref="TestTokenFactory"/> ký — KHÔNG cần identity-service chạy thật:
/// <c>PostConfigure&lt;JwtBearerOptions&gt;</c> thay <c>ConfigurationManager</c> (vốn tải JWKS qua
/// mạng) bằng <see cref="StaticConfigurationManager{T}"/> bọc khoá công khai cục bộ.
///
/// ⚠️ Lệch so với gợi ý ở §5.2.2 của hợp đồng thực thi ("gán <c>options.Configuration</c> trực
/// tiếp + <c>options.ConfigurationManager = null</c>") — đã KIỂM CHỨNG bằng mã nguồn
/// <c>JwtBearerHandler.SetupTokenValidationParametersAsync</c> (aspnetcore v10.0.0): handler
/// CHỈ đọc <c>Options.ConfigurationManager</c> (ném xuống <c>TokenValidationParameters.ConfigurationManager</c>
/// khi là <c>BaseConfigurationManager</c>), KHÔNG BAO GIỜ đọc <c>Options.Configuration</c> trực
/// tiếp lúc xác thực — set <c>Configuration</c> rồi null hoá <c>ConfigurationManager</c> khiến
/// handler nhận "No security keys were provided" (IDX10500) dù khoá đúng. Đã báo lại theo CLAUDE.md
/// "Lệch → báo lại, không tự đổi nghiệp vụ" — xem bàn giao F3.
/// </summary>
public sealed class ChineseDbApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "https://id.antfarms.xyz.test";
    public const string Audience = "af-chinese";

    /// <summary>Email gán admin lúc provision lần đầu (R-P6) — dùng để test nhánh admin mà không cần thao tác DB trực tiếp.</summary>
    public const string BootstrapAdminEmail = "admin-bootstrap@vidu.com";

    public TestTokenFactory TokenFactory { get; } = new(Issuer);

    /// <summary>
    /// F5: thay <c>TimeProvider.System</c> (Program.cs) bằng đồng hồ giả điều khiển được — cần cho
    /// R5-11 (kiểm giờ phiên luyện thanh so với "bây giờ") và local_date lúc 06:30 sáng giờ VN
    /// (§7 tiêu chí F5). Bắt đầu ở "bây giờ" thật để các test KHÔNG liên quan tới thời gian (vd
    /// MeTests, AdminPingTests) không bị ảnh hưởng.
    /// </summary>
    public FakeTimeProvider TimeProvider { get; } = new(DateTimeOffset.UtcNow);

    public ChineseDbApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", TestDatabase.BuildConnectionString("af_chinese_test"));
        Environment.SetEnvironmentVariable("AutoMigrate", "false"); // ChineseDbFixture đã migrate + seed MỘT LẦN cho cả collection

        Environment.SetEnvironmentVariable("Auth__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Auth__Audience", Audience);
        // Không dùng thật — ConfigurationManager bị PostConfigure thay bằng khoá cục bộ ở dưới,
        // chỉ cần khác rỗng để AddAfJwtBearer(AfAuthOptions) không ném lúc khởi động.
        Environment.SetEnvironmentVariable("Auth__JwksUrl", "http://localhost:65535/.well-known/jwks.json");
        Environment.SetEnvironmentVariable("Auth__RequireHttpsMetadata", "false");

        Environment.SetEnvironmentVariable("ChineseAccess__DefaultRoles__0", "learner");
        Environment.SetEnvironmentVariable("ChineseAdmin__BootstrapEmails__0", BootstrapAdminEmail);
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

            // F5: gỡ TimeProvider.System (Program.cs AddSingleton) — nạp SAU AddInfrastructure/
            // AddApplication trong pipeline gốc nên phải Remove rồi Add lại (không PostConfigure
            // được vì TimeProvider không phải Options).
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TimeProvider);
        });
    }
}
