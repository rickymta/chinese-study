namespace AntFarm.Cms.Application.Access.Dtos;

/// <summary>Một dòng trong <c>GET /api/admin/users</c>/<c>GET /api/admin/users/{id}</c>/<c>PUT .../roles</c> (§6.1).</summary>
public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool IsBootstrapAdmin,
    DateTime FirstSeenAt,
    DateTime LastSeenAt);

/// <summary>Trang kết quả <c>GET /api/admin/users</c> (§6.1).</summary>
public sealed record AdminUsersPageDto(
    IReadOnlyList<AdminUserDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

/// <summary>Query string <c>GET /api/admin/users?q=&amp;page=&amp;pageSize=</c> — property có giá trị mặc định để thiếu tham số vẫn bind được.</summary>
public sealed class AdminUsersQuery
{
    public string? Q { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>Body <c>PUT /api/admin/users/{id}/roles</c> — cho phép mảng rỗng (gỡ hết quyền).</summary>
public sealed record SetUserRolesRequest(IReadOnlyList<string>? Roles);

/// <summary>Một dòng <c>GET /api/admin/roles</c> (§6.1) — KHÔNG có trường mô tả (khác chinese-backend: hợp đồng W1 §6.1 chỉ liệt kê code/name/permissions).</summary>
public sealed record RoleDto(string Code, string Name, IReadOnlyList<string> Permissions);
