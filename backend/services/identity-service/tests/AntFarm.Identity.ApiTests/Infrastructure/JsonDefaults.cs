using System.Text.Json;

namespace AntFarm.Identity.ApiTests.Infrastructure;

/// <summary>camelCase + case-insensitive cho cả gửi lẫn đọc JSON trong test — khớp cấu hình JsonNamingPolicy.CamelCase của service (Program.cs), tránh lỗi map property im lặng (JsonSerializerOptions mặc định KHÔNG case-insensitive).</summary>
internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
