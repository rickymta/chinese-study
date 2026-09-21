namespace AntFarm.Identity.Application.Admin;

/// <summary>Query <c>GET /internal/accounts</c> (§6.5) — chỉ kiểm ĐỊNH DẠNG qua <see cref="AdminAccountsQueryValidator"/>.</summary>
public sealed class AdminAccountsQuery
{
    public string? Q { get; init; }

    /// <summary>"active" | "disabled" | "locked" | null (rỗng/giá trị lạ ⇒ không lọc).</summary>
    public string? Status { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record AdminAccountListItemDto(
    Guid Id,
    string Email,
    string DisplayName,
    string TimeZone,
    bool IsActive,
    DateTime? LockedUntil,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

/// <summary>Chi tiết <c>GET /internal/accounts/{id}</c> — thêm <see cref="PasswordChangedAt"/>/<see cref="ActiveSessionCount"/> so với dòng danh sách.</summary>
public sealed record AdminAccountDto(
    Guid Id,
    string Email,
    string DisplayName,
    string TimeZone,
    bool IsActive,
    DateTime? LockedUntil,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime PasswordChangedAt,
    int ActiveSessionCount);

public sealed record AdminAccountListResult(IReadOnlyList<AdminAccountListItemDto> Items, int Page, int PageSize, int TotalCount);

/// <summary>Body <c>POST /internal/accounts/{id}/reset-password</c> — <see cref="NewPassword"/> rỗng/thiếu ⇒ sinh mật khẩu tạm.</summary>
public sealed record ResetPasswordRequest(string? NewPassword);

/// <summary><see cref="TemporaryPassword"/> null khi admin tự nhập <c>newPassword</c> (§6.5).</summary>
public sealed record ResetPasswordResult(string? TemporaryPassword, int RevokedSessions);

public sealed record RevokeSessionsResult(int RevokedSessions);

public sealed record RegistrationDayDto(DateOnly Date, int Count);

public sealed record RegistrationStatsDto(
    string TimeZone,
    IReadOnlyList<RegistrationDayDto> Days,
    int TotalAccounts,
    int DisabledAccounts,
    int ActiveLast7Days,
    int ActiveLast30Days);

/// <summary><c>Source</c>: "database" (đã có dòng identity.settings) | "configuration" (chưa có ⇒ dùng Auth:AllowRegistration).</summary>
public sealed record RegistrationStateDto(bool Enabled, string Source, DateTime? UpdatedAt);

public sealed record UpdateRegistrationRequest(bool Enabled);
