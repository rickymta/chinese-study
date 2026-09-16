namespace AntFarm.Chinese.Application.Access.Dtos;

/// <summary>Một dòng trong <c>GET /api/admin/users</c>/<c>GET /api/admin/users/{id}</c>/<c>PUT .../roles</c> (§6.3).</summary>
public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool IsBootstrapAdmin,
    DateTime FirstSeenAt,
    DateTime LastSeenAt);

/// <summary>Trang kết quả <c>GET /api/admin/users</c> (§6.3).</summary>
public sealed record AdminUsersPageDto(
    IReadOnlyList<AdminUserDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

/// <summary>Query string <c>GET /api/admin/users?q=&amp;page=&amp;pageSize=</c> — property có giá trị mặc định để thiếu tham số vẫn bind được (R4-6).</summary>
public sealed class AdminUsersQuery
{
    public string? Q { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>Body <c>PUT /api/admin/users/{id}/roles</c> — R4-7 cho phép mảng rỗng (gỡ hết quyền).</summary>
public sealed record SetUserRolesRequest(IReadOnlyList<string>? Roles);

/// <summary>Một dòng <c>GET /api/admin/roles</c> (§6.3, D39 — mô tả lấy từ hằng số code, không migration).</summary>
public sealed record RoleDto(string Code, string Name, string Description, IReadOnlyList<string> Permissions);
