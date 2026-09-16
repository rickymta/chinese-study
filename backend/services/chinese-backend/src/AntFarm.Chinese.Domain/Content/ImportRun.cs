namespace AntFarm.Chinese.Domain.Content;

/// <summary>Nhật ký một lượt nạp học liệu (§5.1.1, §5.2.4) — nguồn để <c>ContentImporter</c> quyết định BỎ QUA lượt nạp không đổi (so <see cref="FileHash"/> + <see cref="ImporterVersion"/> với lần <c>succeeded</c> gần nhất).</summary>
public sealed class ImportRun
{
    public Guid Id { get; private set; }

    /// <summary>'characters' | 'hsk-words' (test dùng hậu tố '-test' để không đụng dữ liệu thật).</summary>
    public string Dataset { get; private set; } = null!;

    public string FileHash { get; private set; } = null!;
    public int ImporterVersion { get; private set; }
    public string Status { get; private set; } = null!;
    public int Inserted { get; private set; }
    public int Updated { get; private set; }
    public int Unchanged { get; private set; }
    public int Invalid { get; private set; }
    public int Protected { get; private set; }
    public string? Error { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime FinishedAt { get; private set; }

    // EF Core cần constructor không tham số.
    private ImportRun()
    {
    }

    public static ImportRun Succeeded(
        string dataset, string fileHash, int importerVersion,
        int inserted, int updated, int unchanged, int invalid, int protectedCount,
        DateTime startedAt, DateTime finishedAt) => new()
    {
        Id = Guid.CreateVersion7(),
        Dataset = dataset,
        FileHash = fileHash,
        ImporterVersion = importerVersion,
        Status = "succeeded",
        Inserted = inserted,
        Updated = updated,
        Unchanged = unchanged,
        Invalid = invalid,
        Protected = protectedCount,
        StartedAt = startedAt,
        FinishedAt = finishedAt
    };

    public static ImportRun Failed(string dataset, string fileHash, int importerVersion, string error, DateTime startedAt, DateTime finishedAt) => new()
    {
        Id = Guid.CreateVersion7(),
        Dataset = dataset,
        FileHash = fileHash,
        ImporterVersion = importerVersion,
        Status = "failed",
        Error = error,
        StartedAt = startedAt,
        FinishedAt = finishedAt
    };
}
