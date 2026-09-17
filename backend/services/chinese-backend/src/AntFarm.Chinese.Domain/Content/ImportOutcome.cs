namespace AntFarm.Chinese.Domain.Content;

/// <summary>
/// Kết quả nạp MỘT dòng học liệu (§5.2.4, R6-11) — dùng chung cho <see cref="Word.ApplyImport"/> và
/// <see cref="Character.ApplyImport"/> (đặt tên chung <c>ImportOutcome</c> thay vì
/// <c>WordImportOutcome</c> riêng như liệt kê ở §5.2.1 — cùng ý nghĩa, dùng lại cho cả hai entity,
/// không phải khác biệt nghiệp vụ). <see cref="Inserted"/> KHÔNG được trả bởi <c>ApplyImport</c>
/// (chỉ gọi trên dòng đã tồn tại) — <c>ContentImporter</c> tự gán khi tạo dòng mới bằng
/// <c>Word.CreateFromImport</c>/<c>Character.CreateFromImport</c>.
/// </summary>
public enum ImportOutcome
{
    Inserted,
    Updated,
    Unchanged,
    UpdatedProtected
}
