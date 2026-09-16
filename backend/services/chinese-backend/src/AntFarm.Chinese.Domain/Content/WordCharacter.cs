namespace AntFarm.Chinese.Domain.Content;

/// <summary>Phân rã một <see cref="Word"/> thành các <see cref="Character"/> theo VỊ TRÍ code point trong <c>Simplified</c> (§5.1.1) — dùng để hiển thị chi tiết chữ ở màn chi tiết từ.</summary>
public sealed class WordCharacter
{
    public Guid WordId { get; private set; }

    /// <summary>0-based, theo thứ tự code point (Rune) của <c>Word.Simplified</c>.</summary>
    public short Position { get; private set; }

    public Guid CharacterId { get; private set; }

    // EF Core cần constructor không tham số.
    private WordCharacter()
    {
    }

    public static WordCharacter Create(Guid wordId, short position, Guid characterId) => new()
    {
        WordId = wordId,
        Position = position,
        CharacterId = characterId
    };
}
