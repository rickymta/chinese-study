using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AntFarm.Chinese.ApiTests.Infrastructure;

/// <summary>
/// Factory môi trường "Development" (KHÔNG cần PostgreSQL thật, giống <see cref="ChineseApiFactory"/>)
/// — chỉ để kiểm <c>/openapi/v1.json</c> + <c>/scalar/v1</c> có <c>AllowAnonymous</c> đúng.
/// Review F3 17/09/2026: <c>FallbackPolicy = RequireAuthenticatedUser</c> vô tình chặn hai endpoint
/// tài liệu API này (401) vì chúng chỉ được map bên trong <c>if (app.Environment.IsDevelopment())</c>
/// mà thiếu <c>.AllowAnonymous()</c>.
/// </summary>
public sealed class ChineseDevEnvironmentApiFactory : WebApplicationFactory<Program>
{
    public ChineseDevEnvironmentApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=unused");
        Environment.SetEnvironmentVariable("AutoMigrate", "false");
        Environment.SetEnvironmentVariable("Auth__Issuer", "https://id.antfarms.xyz.test");
        Environment.SetEnvironmentVariable("Auth__Audience", "af-chinese");
        Environment.SetEnvironmentVariable("Auth__JwksUrl", "http://localhost:65535/.well-known/jwks.json");
        Environment.SetEnvironmentVariable("Auth__RequireHttpsMetadata", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}
