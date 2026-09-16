namespace AntFarm.Auth;

/// <summary>
/// Cấu hình tối thiểu để kiểm JWT RS256 phát hành bởi identity-service (§5.2.2).
/// Service ngôn ngữ bind từ section "Auth" của appsettings (Issuer/Audience/JwksUrl/
/// RequireHttpsMetadata) — xem <see cref="JwtBearerExtensions.AddAfJwtBearer(Microsoft.Extensions.DependencyInjection.IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration)"/>.
/// identity-service tự kiểm token CỦA CHÍNH MÌNH (endpoint /api/account) thì dựng
/// <see cref="AfAuthOptions"/> thủ công (Audience cố định "af-identity") vì section "Auth"
/// của identity-service đã dùng cho cấu hình cookie/CORS, không phải JWT bearer.
/// </summary>
public sealed class AfAuthOptions
{
    /// <summary>Phải khớp CHÍNH XÁC "iss" trong token (Jwt:Issuer của identity-service).</summary>
    public required string Issuer { get; init; }

    /// <summary>Một audience mà service này chấp nhận (JWT có thể có nhiều "aud", chỉ cần khớp một).</summary>
    public required string Audience { get; init; }

    /// <summary>URL JWKS nội bộ, vd http://identity-service:8080/.well-known/jwks.json. Bỏ trống khi dùng khoá cục bộ (identity-service tự kiểm token của mình).</summary>
    public string? JwksUrl { get; init; }

    /// <summary>
    /// KHÔNG suy ra từ tên môi trường (Development/Production) — chỉ phụ thuộc <see cref="JwksUrl"/>
    /// có phải https hay không: true khi JwksUrl là https; **false khi JwksUrl là http NỘI BỘ**
    /// (vd Docker af-net <c>http://identity-service:8080/.well-known/jwks.json</c> — production
    /// thật vẫn dùng http ở đây vì không ra Internet). Mặc định true (an toàn) — service tự kiểm
    /// token qua mạng nội bộ PHẢI đặt tường minh false, không thì <see cref="JwtBearerExtensions"/>
    /// dùng <c>HttpDocumentRetriever { RequireHttps = true }</c> từ chối tải JWKS ⇒ 401 toàn bộ.
    /// </summary>
    public bool RequireHttpsMetadata { get; init; } = true;
}
