using AntFarm.Core.Errors;
using Microsoft.AspNetCore.Http;

namespace AntFarm.Identity.Api.Internal;

/// <summary>
/// Đọc <c>X-Actor-Id</c>/<c>X-Actor-Email</c> — người gọi (cms-backend) tự gắn từ claim người
/// dùng THẬT của họ. KHÔNG phải xác thực (đã xác thực bằng <c>X-Service-Key</c> ở
/// <see cref="InternalAccessMiddleware"/>) — chỉ định danh AI đứng sau thao tác để ghi log +
/// kiểm R-W17 (actor ≠ target). Bắt buộc với mọi POST/PUT /internal/*, GET không cần.
/// </summary>
public readonly record struct InternalActor(Guid Id, string Email)
{
    public static InternalActor FromHeaders(HttpRequest request)
    {
        var idRaw = request.Headers["X-Actor-Id"].ToString();
        var email = request.Headers["X-Actor-Email"].ToString();

        var errors = new Dictionary<string, string[]>();
        if (!Guid.TryParse(idRaw, out var id))
            errors["X-Actor-Id"] = ["Thiếu hoặc không đúng định dạng GUID."];
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
            errors["X-Actor-Email"] = ["Thiếu hoặc quá 254 ký tự."];

        if (errors.Count > 0)
            throw new ValidationAppException("Thiếu thông tin người thao tác.", errors);

        return new InternalActor(id, email);
    }
}
