using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AntFarm.Auth.Authorization;

/// <summary>
/// Dịch policy "Permission:&lt;mã quyền&gt;" (do <see cref="RequirePermissionAttribute"/> khai)
/// thành <see cref="AuthorizationPolicy"/> có <see cref="PermissionRequirement"/> tương ứng —
/// không cần đăng ký thủ công từng policy trong <c>AddAuthorization</c>. Policy khác (không có
/// tiền tố) uỷ quyền lại cho provider mặc định.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string Prefix = "Permission:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var permission = policyName[Prefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
