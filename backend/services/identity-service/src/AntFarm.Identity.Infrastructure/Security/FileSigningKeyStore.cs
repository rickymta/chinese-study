using System.Security.Cryptography;
using AntFarm.Identity.Application.Common.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace AntFarm.Identity.Infrastructure.Security;

/// <summary>
/// Khoá ký RSA persist file PEM trong <c>Jwt:KeysPath</c> (R-A13). "kid" = tên file không đuôi,
/// dạng <c>yyyyMMdd-&lt;8 hex&gt;</c> — sắp xếp CHUỖI trùng sắp xếp THEO NGÀY nên file mới nhất
/// luôn đứng cuối khi OrderBy tên file, dùng làm khoá ký mặc định khi <c>Jwt:ActiveKeyId</c> trống.
/// </summary>
public sealed class FileSigningKeyStore : ISigningKeyStore
{
    private readonly string _keysPath;
    private readonly string? _activeKeyId;
    private readonly Dictionary<string, RSA> _keysByKid;

    /// <param name="keysPath">Đường dẫn TUYỆT ĐỐI đã được resolve (Program.cs quy đổi từ cấu hình tương đối theo ContentRootPath) — lớp này không tự đoán thư mục làm việc.</param>
    /// <param name="activeKeyId">"Jwt:ActiveKeyId" — trống thì dùng file mới nhất.</param>
    /// <param name="isDevelopment">Development: thư mục trống ⇒ tự sinh khoá mới. Môi trường khác: thư mục trống ⇒ ném lỗi ngay lúc khởi động (R-A13).</param>
    public FileSigningKeyStore(string keysPath, string? activeKeyId, bool isDevelopment)
    {
        _keysPath = keysPath;
        _activeKeyId = string.IsNullOrWhiteSpace(activeKeyId) ? null : activeKeyId;

        Directory.CreateDirectory(_keysPath);
        var files = Directory.EnumerateFiles(_keysPath, "*.pem").OrderBy(f => f, StringComparer.Ordinal).ToList();

        if (files.Count == 0)
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy khoá ký RS256 nào trong '{_keysPath}'. Đây không phải môi trường Development nên " +
                    "hệ thống KHÔNG tự sinh khoá — hãy đặt file .pem hợp lệ vào thư mục này (xem README, R-A13) rồi khởi động lại.");
            }

            files = [GenerateNewKeyFile(_keysPath)];
        }

        _keysByKid = files.ToDictionary(f => Path.GetFileNameWithoutExtension(f), LoadRsaFromPemFile);
    }

    public SigningCredentials GetActiveSigningCredentials()
    {
        var kid = _activeKeyId ?? _keysByKid.Keys.OrderBy(k => k, StringComparer.Ordinal).Last();
        if (!_keysByKid.TryGetValue(kid, out var rsa))
        {
            throw new InvalidOperationException(
                $"Jwt:ActiveKeyId='{kid}' nhưng không tìm thấy file '{kid}.pem' trong '{_keysPath}'.");
        }

        return new SigningCredentials(new RsaSecurityKey(rsa) { KeyId = kid }, SecurityAlgorithms.RsaSha256);
    }

    public IEnumerable<SecurityKey> GetPublicKeys()
        => _keysByKid.Select(kv => new RsaSecurityKey(kv.Value) { KeyId = kv.Key });

    public IReadOnlyList<JsonWebKey> GetPublicJsonWebKeys()
        => _keysByKid.Select(kv =>
        {
            var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(kv.Value) { KeyId = kv.Key });
            jwk.Use = "sig";
            jwk.Alg = SecurityAlgorithms.RsaSha256;
            jwk.KeyId = kv.Key;
            return jwk;
        }).ToList();

    private static string GenerateNewKeyFile(string keysPath)
    {
        var kid = $"{DateTime.UtcNow:yyyyMMdd}-{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4))}";
        using var rsa = RSA.Create(2048);
        var path = Path.Combine(keysPath, $"{kid}.pem");
        File.WriteAllText(path, rsa.ExportPkcs8PrivateKeyPem());
        return path;
    }

    private static RSA LoadRsaFromPemFile(string path)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(path));
        return rsa;
    }
}
