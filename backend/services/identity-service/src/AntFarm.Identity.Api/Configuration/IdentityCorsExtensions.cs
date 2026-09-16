using AntFarm.Identity.Application.Common.Options;

namespace AntFarm.Identity.Api.Configuration;

/// <summary>
/// CORS RIÊNG của identity-service (KHÔNG dùng <c>AntFarm.Security.Cors.AddAfCors</c>/
/// <c>UseAfCors</c>) — endpoint xác thực cần <c>AllowCredentials()</c> để trình duyệt gửi/nhận
/// cookie <c>af_rt</c> cross-site (<c>chinese.antfarms.xyz</c> ↔ <c>id.antfarms.xyz</c>, R-A7b);
/// policy dùng chung của AntFarm.Security không bật credentials vì hầu hết service khác luôn
/// cùng-origin (R-N9), không cần.
/// </summary>
public static class IdentityCorsExtensions
{
    public const string PolicyName = "IdentityCors";

    public static IServiceCollection AddIdentityCors(this IServiceCollection services, AuthOptions authOptions)
    {
        services.AddCors(options => options.AddPolicy(PolicyName, policy =>
        {
            if (authOptions.AllowedOrigins.Length == 0)
            {
                policy.WithOrigins(); // rỗng ⇒ chặn hết, an toàn theo mặc định (giống AntFarm.Security.Cors)
                return;
            }

            policy.WithOrigins(authOptions.AllowedOrigins)
                .AllowCredentials()
                .AllowAnyHeader()
                .WithMethods("GET", "POST", "PUT", "OPTIONS")
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        }));

        return services;
    }

    public static IApplicationBuilder UseIdentityCors(this IApplicationBuilder app) => app.UseCors(PolicyName);
}
