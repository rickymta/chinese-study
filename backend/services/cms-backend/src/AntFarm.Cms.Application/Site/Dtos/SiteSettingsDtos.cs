namespace AntFarm.Cms.Application.Site.Dtos;

/// <summary>Trả về từ GET/PUT <c>/api/admin/site-settings</c> (§6.2) — <see cref="UpdatedAt"/> = mốc sửa GẦN NHẤT trong 11 khoá.</summary>
public sealed record SiteSettingsDto(IReadOnlyDictionary<string, string> Values, DateTime UpdatedAt);

/// <summary>Thân <c>PUT /api/admin/site-settings</c> — phải gửi ĐỦ 11 khoá (§5.2.3, kiểm bởi <c>UpdateSiteSettingsRequestValidator</c>).</summary>
public sealed record UpdateSiteSettingsRequest(Dictionary<string, string>? Values);
