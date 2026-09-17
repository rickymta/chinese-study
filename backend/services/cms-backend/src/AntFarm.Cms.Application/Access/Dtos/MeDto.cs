namespace AntFarm.Cms.Application.Access.Dtos;

/// <summary>Shape của GET /api/me (§6.1) — camelCase qua JsonNamingPolicy cấu hình chung ở Program.cs.</summary>
public sealed record MeDto(
    Guid Id,
    string Email,
    string DisplayName,
    string TimeZone,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    DateTime FirstSeenAt);
