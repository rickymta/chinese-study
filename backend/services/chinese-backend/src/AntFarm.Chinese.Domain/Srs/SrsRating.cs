namespace AntFarm.Chinese.Domain.Srs;

/// <summary>
/// Bốn mức tự chấm của người học sau khi lật thẻ (§3.4 R7-1). Giá trị số trùng với
/// <c>Rating</c> (IntEnum 1..4) của py-fsrs — các công thức FSRS dùng thẳng giá trị số này
/// (vd <c>G − 3</c>) nên KHÔNG được đổi thứ tự/giá trị.
/// </summary>
public enum SrsRating
{
    Again = 1,
    Hard = 2,
    Good = 3,
    Easy = 4
}
