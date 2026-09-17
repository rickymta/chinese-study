/** Ký tự thay thế chữ đang luyện (gạch dưới toàn角 — cùng bề rộng chữ Hán nên từ không bị co). */
export const MASK_CHAR = '＿'

/**
 * Che chữ đang luyện trong một từ (bước Tự viết — review F8): `爱人` với chữ `爱` ⇒ `＿人`. Người học vẫn thấy pinyin +
 * nghĩa để nhớ ngữ cảnh nhưng không chép được chữ mẫu — nếu không, lượt "viết sạch" không còn ý nghĩa.
 * Duyệt theo code point (chữ mở rộng B là cặp surrogate); chữ xuất hiện nhiều lần đều bị che.
 */
export function maskHanzi(word: string, hanzi: string): string {
  if (!hanzi) return word
  return Array.from(word)
    .map((cp) => (cp === hanzi ? MASK_CHAR : cp))
    .join('')
}
