namespace AntFarm.Chinese.Domain.Learning;

/// <summary>Bước luyện viết đã GHI vào DB (§3.2 R-W2) — bước "Xem" không ghi gì (chỉ hoạt hình, không phải một lần viết).</summary>
public static class WritingModes
{
    /// <summary>Bước "Tô theo" — <c>quiz({ showOutline: true })</c>, tự hiện nét đúng sau 2 lần sai.</summary>
    public const string Guided = "guided";

    /// <summary>Bước "Tự viết" — <c>quiz({ showOutline: false, showCharacter: false })</c>, tự hiện sau 3 lần sai. Chỉ mode này mới tính "sạch" cho R-W5.</summary>
    public const string Recall = "recall";

    private static readonly HashSet<string> Known = [Guided, Recall];

    public static bool IsKnown(string mode) => Known.Contains(mode);
}
