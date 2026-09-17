using System.Text.Json;

namespace AntFarm.Chinese.ApiTests.Infrastructure;

/// <summary>camelCase + case-insensitive cho cả gửi lẫn đọc JSON trong test — khớp cấu hình JsonNamingPolicy.CamelCase của service (Program.cs).</summary>
internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
