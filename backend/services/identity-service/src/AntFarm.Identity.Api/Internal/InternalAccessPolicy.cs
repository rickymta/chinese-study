using System.Security.Cryptography;
using System.Text;
using AntFarm.Identity.Application.Common.Options;

namespace AntFarm.Identity.Api.Internal;

/// <summary>
/// Chốt chặn R-W4 (§5.2.9) — hàm THUẦN (không đụng HttpContext) để unit test được đầy đủ ma trận
/// mà không cần dựng TestServer. <see cref="InternalAccessMiddleware"/> chỉ là lớp mỏng gọi hàm
/// này rồi dịch quyết định thành response HTTP.
/// </summary>
public static class InternalAccessPolicy
{
    public static InternalAccessDecision Evaluate(bool isInternalPath, int localPort, InternalOptions options, string? providedKey)
    {
        // API nội bộ tắt (Port=0 hoặc ServiceKey<32 ký tự, §5.2.9): route /internal/* không tồn
        // tại theo mắt người gọi (404), route khác đi tiếp bình thường — kể cả khi có key đúng
        // (RW1: không lộ chuyện API nội bộ "có tồn tại nhưng đang tắt").
        if (!options.IsEnabled)
            return isInternalPath ? InternalAccessDecision.NotFound : InternalAccessDecision.PassThrough;

        if (localPort == options.Port)
        {
            // Cổng nội bộ CHỈ phục vụ /internal/* — mọi route công khác (kể cả health/JWKS) ⇒ 404.
            if (!isInternalPath)
                return InternalAccessDecision.NotFound;

            return IsKeyValid(options.ServiceKey, providedKey)
                ? InternalAccessDecision.Allow
                : InternalAccessDecision.Unauthorized;
        }

        // Cổng công khai KHÔNG BAO GIỜ phục vụ /internal/* — kể cả key đúng (RW1: chặn nhầm cấu
        // hình gateway/nginx lộ route này ra Internet).
        return isInternalPath ? InternalAccessDecision.NotFound : InternalAccessDecision.PassThrough;
    }

    /// <summary>So khớp thời gian không đổi (<see cref="CryptographicOperations.FixedTimeEquals"/>) — tránh timing attack dò từng byte khoá dịch vụ; độ dài khác nhau ⇒ sai ngay (không so buffer khác kích thước).</summary>
    private static bool IsKeyValid(string expected, string? provided)
    {
        if (string.IsNullOrEmpty(provided))
            return false;

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        if (expectedBytes.Length != providedBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
