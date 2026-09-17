namespace AntFarm.Chinese.Domain.Lessons;

/// <summary>Một khối nội dung của bài học (§5.1.1) — <see cref="Payload"/> là jsonb, hình dạng tuỳ <see cref="Type"/> (xem <c>Payloads.cs</c>).</summary>
public sealed class LessonBlock
{
    public Guid Id { get; private set; }
    public Guid LessonId { get; private set; }
    public short OrderIndex { get; private set; }
    public string Type { get; private set; } = null!;
    public string Payload { get; private set; } = null!;

    // EF Core cần constructor không tham số.
    private LessonBlock()
    {
    }

    public static LessonBlock Create(Guid lessonId, short orderIndex, string type, string payloadJson) => new()
    {
        Id = Guid.CreateVersion7(),
        LessonId = lessonId,
        OrderIndex = orderIndex,
        Type = type,
        Payload = payloadJson
    };
}
