namespace AntFarm.Chinese.Infrastructure.Content;

public enum ImportRunStatus { Skipped, Succeeded, Failed }

/// <summary>Kết quả trả về của <c>ContentImporter.Import*Async</c> (§5.2.4) — dùng để log/kiểm thử, KHÔNG lưu DB trực tiếp (đã ghi vào <c>content.import_runs</c> bên trong).</summary>
public sealed record ImportSummary(ImportRunStatus Status, int Inserted, int Updated, int Unchanged, int Invalid, int Protected, string? Error = null)
{
    public static ImportSummary Skipped() => new(ImportRunStatus.Skipped, 0, 0, 0, 0, 0);

    public static ImportSummary Succeeded(int inserted, int updated, int unchanged, int invalid, int protectedCount) =>
        new(ImportRunStatus.Succeeded, inserted, updated, unchanged, invalid, protectedCount);

    public static ImportSummary Failed(string error) => new(ImportRunStatus.Failed, 0, 0, 0, 0, 0, error);
}
