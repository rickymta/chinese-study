namespace AntFarm.Chinese.Domain.Srs;

/// <summary>Danh mục kiểu thẻ SRS (§5.1.2) — F7 chỉ có một kiểu; tách hằng số để không gõ tay chuỗi rải rác.</summary>
public static class SrsCardTypes
{
    public const string HanziToMeaning = "hanzi_to_meaning";
}

/// <summary>Nguồn tạo thẻ (§5.1.2, R7-2, K12/R-LS5): <c>path</c> (tạo lười theo lộ trình HSK), <c>manual</c> (người học tự thêm từ tra từ), <c>lesson</c> (F9 — hoàn thành bài học lần đầu).</summary>
public static class SrsCardSources
{
    public const string Path = "path";
    public const string Manual = "manual";
    public const string Lesson = "lesson";

    public static readonly IReadOnlyList<string> All = [Path, Manual, Lesson];
}
