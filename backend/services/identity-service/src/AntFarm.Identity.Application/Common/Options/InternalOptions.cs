namespace AntFarm.Identity.Application.Common.Options;

/// <summary>
/// Bind từ section "Internal" (§5.2.9, W10) — API nội bộ <c>/internal/*</c> phục vụ quản trị tài
/// khoản/cài đặt, chỉ nghe trên CỔNG RIÊNG (dev 5291, Docker 8081), xác thực bằng khoá dịch vụ
/// tĩnh <see cref="ServiceKey"/> (header <c>X-Service-Key</c>) — KHÔNG JWT, KHÔNG CORS.
/// </summary>
public sealed class InternalOptions
{
    public int Port { get; init; }
    public string ServiceKey { get; init; } = "";

    /// <summary>
    /// R-W4: bật khi có cổng riêng VÀ khoá đủ dài (≥ 32 ký tự). Khoá ngắn/rỗng ⇒ coi như CHƯA
    /// cấu hình — service vẫn khởi động bình thường, chỉ API nội bộ tắt (mọi request /internal/*
    /// trả 404) thay vì ném ngoại lệ dừng tiến trình.
    /// </summary>
    public bool IsEnabled => Port > 0 && ServiceKey.Length >= 32;
}
