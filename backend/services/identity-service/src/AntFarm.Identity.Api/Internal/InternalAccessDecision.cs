namespace AntFarm.Identity.Api.Internal;

/// <summary>Kết quả chốt chặn R-W4 (§5.2.9) — xem <see cref="InternalAccessPolicy"/>.</summary>
public enum InternalAccessDecision
{
    /// <summary>Không liên quan API nội bộ — đi tiếp pipeline bình thường (CORS/rate limit/JWT...).</summary>
    PassThrough,

    /// <summary>404 KHÔNG body — không lộ thông tin API nội bộ tồn tại hay không.</summary>
    NotFound,

    /// <summary>401 — đúng cổng nội bộ nhưng thiếu/sai khoá dịch vụ.</summary>
    Unauthorized,

    /// <summary>Đúng cổng nội bộ + đúng khoá — cho qua.</summary>
    Allow
}
