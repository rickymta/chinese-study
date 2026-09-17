using Microsoft.IdentityModel.Tokens;

namespace AntFarm.Identity.Application.Common.Abstractions;

/// <summary>
/// Khoá ký RSA persist file PEM (R-A13). Đọc lại thư mục ở MỖI lần gọi (không cache) để phản
/// ánh đúng khoá hiện có — hỗ trợ xoay khoá (thêm file .pem mới) mà không cần khởi động lại.
/// </summary>
public interface ISigningKeyStore
{
    /// <summary>Khoá dùng để KÝ token mới — <c>Jwt:ActiveKeyId</c> nếu có, không thì file mới nhất.</summary>
    SigningCredentials GetActiveSigningCredentials();

    /// <summary>Public key của MỌI file khoá còn trong thư mục — dùng để tự kiểm token của chính identity-service.</summary>
    IEnumerable<SecurityKey> GetPublicKeys();

    /// <summary>Như <see cref="GetPublicKeys"/> nhưng ở dạng JWK — nguồn cho endpoint <c>/.well-known/jwks.json</c>.</summary>
    IReadOnlyList<JsonWebKey> GetPublicJsonWebKeys();
}
