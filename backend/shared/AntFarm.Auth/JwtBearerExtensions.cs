using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AntFarm.Auth;

/// <summary>
/// Đăng ký xác thực JWT Bearer RS256 dùng chung cho mọi service (identity-service kiểm token
/// của chính mình bằng khoá cục bộ; service ngôn ngữ kiểm bằng JWKS nội bộ của identity-service).
/// R-N5: mỗi service kiểm token TRỰC TIẾP, không tin gateway đã kiểm.
/// </summary>
public static class JwtBearerExtensions
{
    private static readonly string[] AllowedAlgorithms = [SecurityAlgorithms.RsaSha256];

    /// <summary>Overload tiện dụng: bind <see cref="AfAuthOptions"/> từ section "Auth" (dùng bởi service ngôn ngữ, §5.2.0.6 khối "Auth" của F3).</summary>
    public static AuthenticationBuilder AddAfJwtBearer(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Auth").Get<AfAuthOptions>()
            ?? throw new InvalidOperationException(
                "Thiếu cấu hình Auth (Issuer/Audience/JwksUrl) — xem appsettings.Development.json.example.");

        return services.AddAfJwtBearer(options);
    }

    /// <summary>
    /// Kiểm token qua JWKS tải qua MẠNG NỘI BỘ (R-A3) — dùng cho mọi service ngôn ngữ.
    /// <see cref="ConfigurationManager{T}"/> tự làm mới định kỳ + tự tải lại khi gặp "kid" lạ
    /// (RefreshOnIssuerKeyNotFound) nên hỗ trợ xoay khoá ký của identity-service mà không cần
    /// khởi động lại service ngôn ngữ.
    /// </summary>
    public static AuthenticationBuilder AddAfJwtBearer(this IServiceCollection services, AfAuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.JwksUrl))
            throw new InvalidOperationException("AfAuthOptions.JwksUrl bắt buộc khi dùng overload kiểm JWKS qua mạng — dùng overload nhận localKeys nếu tự kiểm token của chính mình.");

        return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                ConfigureCommon(o, options);
                // RefreshOnIssuerKeyNotFound nằm ở JwtBearerOptions (không phải ConfigurationManager<T>)
                // — tự tải lại JWKS khi gặp "kid" lạ, hỗ trợ xoay khoá của identity-service.
                o.RefreshOnIssuerKeyNotFound = true;
                o.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    options.JwksUrl,
                    new JwksConfigurationRetriever(options.Issuer),
                    new HttpDocumentRetriever { RequireHttps = options.RequireHttpsMetadata });
            });
    }

    /// <summary>
    /// Kiểm token bằng khoá CỤC BỘ, không gọi JWKS qua mạng — dùng cho identity-service tự kiểm
    /// token của chính mình (endpoint /api/account, audience "af-identity"). <paramref name="localKeys"/>
    /// được gọi lại ở MỖI lần xác thực (không cache) để phản ánh đúng khoá hiện có trong thư mục
    /// (hỗ trợ xoay khoá không cần khởi động lại — nhất quán với overload JWKS).
    /// </summary>
    public static AuthenticationBuilder AddAfJwtBearer(
        this IServiceCollection services, AfAuthOptions options, Func<IEnumerable<SecurityKey>> localKeys)
    {
        return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                ConfigureCommon(o, options);
                o.TokenValidationParameters.IssuerSigningKeyResolver = (_, _, kid, _) =>
                    localKeys().Where(k => kid is null || k.KeyId == kid);
            });
    }

    private static void ConfigureCommon(JwtBearerOptions o, AfAuthOptions options)
    {
        // MapInboundClaims=false: giữ nguyên tên claim gốc trong token ("sub", "name"...) thay vì
        // .NET tự đổi sang URI claim kiểu WS-Federation cũ (vd "sub" → nameidentifier) — FE và
        // ClaimsPrincipalExtensions đều mong tên claim gốc theo JWT chuẩn.
        o.MapInboundClaims = false;
        o.RequireHttpsMetadata = options.RequireHttpsMetadata;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ValidAlgorithms = AllowedAlgorithms,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name"
        };

        // 401/403 do middleware xác thực sinh ra cũng phải có body JSON thống nhất (§6.0) —
        // mặc định ASP.NET Core trả 401/403 rỗng thân, FE không switch theo "code" được.
        o.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Thiếu hoặc token không hợp lệ.",
                    code = "UNAUTHENTICATED"
                });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Không đủ quyền truy cập.",
                    code = "FORBIDDEN"
                });
            }
        };
    }
}
