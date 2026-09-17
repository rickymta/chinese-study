namespace AntFarm.Chinese.Domain.Srs;

/// <summary>
/// Ánh xạ <see cref="SrsState"/> ⇄ chuỗi snake_case lưu ở DB (§5.1.2) và trả về ở API (§6.2) — MỘT
/// NƠI DUY NHẤT cho cả EF value converter (Infrastructure) lẫn DTO hiển thị (Application, vd khối
/// <c>srs</c> ở chi tiết từ) để không lặp switch rải rác nhiều tầng.
/// </summary>
public static class SrsStateCodes
{
    public const string New = "new";
    public const string Learning = "learning";
    public const string Review = "review";
    public const string Relearning = "relearning";

    public static string ToCode(SrsState state) => state switch
    {
        SrsState.New => New,
        SrsState.Learning => Learning,
        SrsState.Review => Review,
        SrsState.Relearning => Relearning,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Trạng thái SRS không hợp lệ.")
    };

    public static SrsState FromCode(string code) => code switch
    {
        New => SrsState.New,
        Learning => SrsState.Learning,
        Review => SrsState.Review,
        Relearning => SrsState.Relearning,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Mã trạng thái SRS không hợp lệ.")
    };
}
