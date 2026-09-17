namespace AntFarm.Chinese.Domain.Srs;

/// <summary>
/// Trạng thái vòng đời một thẻ SRS (lưu chuỗi snake_case ở tầng Infrastructure — không phải mối
/// quan tâm của Domain). <see cref="New"/> là quy ước riêng của AntFarm cho thẻ chưa ôn lần nào;
/// về mặt thuật toán nó tương đương <c>Learning</c>, bước 0, độ ổn định/độ khó rỗng của py-fsrs
/// (§5.2.6 — "Thẻ new của ta ≡ Learning, step 0, S = D = null").
/// </summary>
public enum SrsState
{
    New,
    Learning,
    Review,
    Relearning
}
