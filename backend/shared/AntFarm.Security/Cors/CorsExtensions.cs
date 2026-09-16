using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AntFarm.Security.Cors;

/// <summary>
/// CORS dùng chung, đọc danh sách origin từ cấu hình "Cors:AllowedOrigins".
/// Danh sách rỗng (mặc định F0) ⇒ không phục vụ CORS cho ai — an toàn theo mặc định,
/// không có nhánh "cho phép mọi origin" như MedDental (AntFarm không có domain gốc
/// dùng chung kiểu *.meddental.vn).
///
/// Từ F2, identity-service tự dựng CORS có credentials riêng từ "Auth:AllowedOrigins"
/// (R-A7b) — không dùng policy này cho các endpoint xác thực.
/// </summary>
public static class CorsExtensions
{
    public const string PolicyName = "AfCors";

    public static IServiceCollection AddAfCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (origins.Length == 0)
                {
                    // Không cấu hình origin nào ⇒ chặn hết (mặc định an toàn).
                    policy.WithOrigins();
                }
                else
                {
                    policy.WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            });
        });

        return services;
    }

    /// <summary>Áp policy AfCors. Gọi sau UseRouting() và trước UseAuthentication().</summary>
    public static IApplicationBuilder UseAfCors(this IApplicationBuilder app)
        => app.UseCors(PolicyName);
}
