namespace AntFarm.Chinese.Domain.Content;

/// <summary>Trạng thái nghĩa tiếng Việt (§5.1.1) — mọi nghĩa mới nạp là <see cref="Machine"/> tới khi được duyệt ở F10.</summary>
public static class MeaningViStatus
{
    public const string Machine = "machine";
    public const string Reviewed = "reviewed";
}

/// <summary>Nguồn nghĩa tiếng Việt (D5, §5.1.1).</summary>
public static class MeaningViSource
{
    public const string Cvdict = "cvdict";
    public const string Machine = "machine";
    public const string Manual = "manual";
}

/// <summary>Trạng thái Hán Việt (R6-7, §5.1.1) — <c>null</c> khi <c>hanViet</c> cũng <c>null</c> (chữ 吗 không có âm Hán Việt).</summary>
public static class HanVietStatus
{
    public const string Derived = "derived";
    public const string Reviewed = "reviewed";
}
