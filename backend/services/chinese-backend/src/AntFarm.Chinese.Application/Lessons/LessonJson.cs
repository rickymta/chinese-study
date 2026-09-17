using System.Text.Json;

namespace AntFarm.Chinese.Application.Lessons;

/// <summary>
/// (De)serialize dùng chung cho mọi cột <c>jsonb</c> của bài học (§5.1) — <c>lesson_blocks.payload</c>,
/// <c>quiz_questions.options</c>, <c>lessons.glossary</c> — camelCase, KHÔNG dùng <see cref="JsonDocument"/>/
/// owned entity trong entity (một cách làm duy nhất cho mọi cột jsonb, §5.1). <see cref="ToElement"/>
/// dựng <see cref="JsonElement"/> ĐỘC LẬP (Clone) để trả thẳng trong DTO chi tiết bài học mà không giữ
/// tham chiếu tới <see cref="JsonDocument"/> gốc (vốn phải <c>Dispose</c>).
/// </summary>
public static class LessonJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

    public static JsonElement ToElement(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
