using System.Text;

namespace AntFarm.Chinese.Application.Writing;

/// <summary>
/// Kiểm một chuỗi là ĐÚNG MỘT chữ Hán đơn — dùng cho <c>hanzi</c> của F8 (§5.2.2 test validator).
/// KHÁC <c>DictionaryQueryParser</c>/<c>LessonContentValidator</c> (chỉ dùng <c>\p{IsCJK...}</c> —
/// .NET KHÔNG hỗ trợ named block property cho các khối NGOÀI BMP) — Ext. B (U+20000–U+2A6DF, lưu
/// dưới dạng surrogate pair trong UTF-16) phải so trực tiếp bằng <see cref="Rune.Value"/>.
/// </summary>
public static class CjkCharacterValidation
{
    private const int UnifiedStart = 0x4E00, UnifiedEnd = 0x9FFF;
    private const int ExtensionAStart = 0x3400, ExtensionAEnd = 0x4DBF;
    private const int ExtensionBStart = 0x20000, ExtensionBEnd = 0x2A6DF;

    public static bool IsSingleCjkCharacter(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        string normalized;
        try
        {
            // string.Normalize NÉM ArgumentException với chuỗi UTF-16 dị dạng (vd một surrogate cao
            // đứng một mình không ghép đôi) — chặn ở đây để trả false thay vì 500 (review F8 17/09/2026).
            normalized = text.Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            return false;
        }

        var runes = normalized.EnumerateRunes().ToList();
        if (runes.Count != 1)
            return false;

        var value = runes[0].Value;
        return (value >= UnifiedStart && value <= UnifiedEnd)
            || (value >= ExtensionAStart && value <= ExtensionAEnd)
            || (value >= ExtensionBStart && value <= ExtensionBEnd);
    }
}
