// Tiện ích kiểm chữ Hán ↔ pinyin cho form soạn bài (khớp luật §5.4.3 phía client, server vẫn là nguồn sự thật).

import { normalizeNumbered, stripPunctuation } from '@/lib/pinyin'

/** Khối CJB Unified Ideographs (U+4E00–U+9FFF) + Ext A (U+3400–U+4DBF) + Ext B (U+20000–U+2A6DF, surrogate pair). */
const HANZI_RE = /[一-鿿㐀-䶿]|[\ud840-\ud869][\udc00-\udfff]/g

/** Số chữ Hán trong chuỗi (bỏ dấu câu, chữ Latin, khoảng trắng). */
export function countHanzi(text: string): number {
  return (text.match(HANZI_RE) ?? []).length
}

/** `true` khi chuỗi (sau khi bỏ dấu câu/khoảng trắng) chỉ gồm chữ Hán và không rỗng. */
export function isHanziOnly(text: string): boolean {
  const core = stripPunctuation(text)
  if (!core) return false
  return countHanzi(core) === Array.from(core).length
}

/** Số âm tiết của pinyin số thanh (bỏ dấu câu dính hai đầu token); token nào sai dạng ⇒ `null`. */
export function countPinyinSyllables(pinyin: string): number | null {
  const cleaned = stripPunctuationTokens(pinyin)
  if (!cleaned) return 0
  const normalized = normalizeNumbered(cleaned)
  if (!normalized) return null
  return normalized.split(' ').length
}

/** Bỏ dấu câu dính đầu/cuối mỗi token pinyin (`'Ni3 hao3!'` ⇒ `'Ni3 hao3'`), giữ token rỗng ra ngoài. */
function stripPunctuationTokens(pinyin: string): string {
  return pinyin
    .trim()
    .split(/\s+/)
    .map((t) => stripPunctuation(t))
    .filter(Boolean)
    .join(' ')
}

/**
 * Kiểm một cặp chữ Hán + pinyin của dòng hội thoại/ví dụ/glossary: trả thông điệp lỗi tiếng Việt hoặc `null`.
 * Luật: pinyin đúng dạng số thanh (sau khi bỏ dấu câu), số âm tiết = số chữ Hán.
 */
export function pinyinProblem(hanzi: string, pinyin: string): string | null {
  if (!pinyin.trim()) return 'Chưa nhập pinyin.'
  const syllables = countPinyinSyllables(pinyin)
  if (syllables === null) return 'Pinyin chưa đúng dạng số thanh (vd ni3 hao3, thanh nhẹ 5, ü viết v).'
  const chars = countHanzi(hanzi)
  if (chars > 0 && syllables !== chars) return `Số âm tiết (${syllables}) khác số chữ Hán (${chars}).`
  return null
}
