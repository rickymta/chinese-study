import { normalizeNumbered } from '@af/chinese-kit'

/** Khoá so sánh pinyin số: chuẩn hoá (`ü` ⇒ `v`, gộp khoảng trắng) rồi về chữ thường. */
const toKey = (s: string): string => (normalizeNumbered(s) ?? s.trim()).toLowerCase()

/**
 * Cách đọc (pinyin số) của chữ ở vị trí `index` trong từ: ưu tiên âm tiết tương ứng trong `wordPinyin`
 * (`hao3` của 你好 ở vị trí 1) nếu nó nằm trong `readings` của chữ; không thì lấy cách đọc đầu tiên; chữ không có
 * cách đọc ⇒ `null`. Âm tiết `r5` (nhi hoá) không có chữ riêng nên bị bỏ khi đếm vị trí (`na3 r5` ⇒ 哪 = `na3`).
 */
export function readingAt(wordPinyin: string, index: number, readings: readonly string[] | null | undefined): string | null {
  const list = (readings ?? []).map(toKey).filter(Boolean)
  const syllables = wordPinyin
    .normalize('NFC')
    .trim()
    .split(/\s+/)
    .filter((s) => s && s.toLowerCase() !== 'r5')
  const want = syllables[index] ? toKey(syllables[index]!) : null
  if (want && list.includes(want)) return want
  // Chữ không có danh sách cách đọc (dữ liệu thiếu) ⇒ vẫn dùng âm tiết trong từ.
  if (want && list.length === 0) return want
  return list[0] ?? null
}
