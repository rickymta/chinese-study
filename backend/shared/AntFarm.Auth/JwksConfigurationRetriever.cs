using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AntFarm.Auth;

/// <summary>
/// Tải danh sách khoá công khai từ một endpoint JWKS THUẦN (RFC 7517) — không phải endpoint
/// discovery OpenID Connect đầy đủ. <see cref="ConfigurationManager{T}"/> của IdentityModel chỉ
/// biết đọc <see cref="OpenIdConnectConfiguration"/> nên bọc JWKS vào một cấu hình rỗng, chỉ có
/// Issuer (đặt tường minh, không đọc từ JWKS) + SigningKeys — đủ cho JwtBearer xác thực chữ ký,
/// tự làm mới định kỳ, tự thử lại khi gặp "kid" lạ (xoay khoá).
/// </summary>
/// <param name="issuer">Issuer kỳ vọng — JWKS thuần không tự công bố issuer nên phải truyền vào.</param>
public sealed class JwksConfigurationRetriever(string issuer) : IConfigurationRetriever<OpenIdConnectConfiguration>
{
    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        string address, IDocumentRetriever retriever, CancellationToken cancel)
    {
        var json = await retriever.GetDocumentAsync(address, cancel).ConfigureAwait(false);
        var keySet = new JsonWebKeySet(json);

        var configuration = new OpenIdConnectConfiguration { Issuer = issuer };
        foreach (var key in keySet.GetSigningKeys())
            configuration.SigningKeys.Add(key);

        return configuration;
    }
}
