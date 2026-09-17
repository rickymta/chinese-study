// Tiện ích pinyin của app tiếng Trung (hợp đồng F5 §5.3.C) — nguồn sự thật cho HIỂN THỊ.
// Quy ước dự án: pinyin LƯU dạng số thanh (`ni3 hao3`, thanh nhẹ `5`, `ü` = `v`), HIỂN THỊ dạng dấu (`nǐ hǎo`).
// Hàm thuần, không phụ thuộc React — test bằng vitest (`pinyin.test.ts`).

export type Tone = 1 | 2 | 3 | 4 | 5

export interface ParsedSyllable {
  /** Chữ thường, `ü` viết `v`. */
  letters: string
  tone: Tone
  /** Chữ cái đầu viết hoa (tên riêng, đầu câu). */
  capitalized: boolean
}

/** Dấu thanh Unicode có sẵn (precomposed) cho 6 nguyên âm, thứ tự thanh 1–4. */
export const TONE_MARKS: Record<'a' | 'e' | 'i' | 'o' | 'u' | 'v', [string, string, string, string]> = {
  a: ['ā', 'á', 'ǎ', 'à'],
  e: ['ē', 'é', 'ě', 'è'],
  i: ['ī', 'í', 'ǐ', 'ì'],
  o: ['ō', 'ó', 'ǒ', 'ò'],
  u: ['ū', 'ú', 'ǔ', 'ù'],
  v: ['ǖ', 'ǘ', 'ǚ', 'ǜ'],
}

/** Bảng ngược: ký tự có dấu ⇒ { nguyên âm gốc, thanh } (cả chữ hoa). */
const MARK_TO_BASE: Record<string, { base: string; tone: Tone }> = (() => {
  const out: Record<string, { base: string; tone: Tone }> = {}
  for (const [vowel, marks] of Object.entries(TONE_MARKS)) {
    marks.forEach((m, i) => {
      const tone = (i + 1) as Tone
      out[m] = { base: vowel, tone }
      out[m.toUpperCase()] = { base: vowel.toUpperCase(), tone }
    })
  }
  return out
})()

const NUMBERED_TOKEN = /^([A-Za-z]+)([1-5])$/

/** `ü`, `u:` (cả hoa) ⇒ `v`/`V` — dạng lưu trữ của dự án. */
function replaceUmlaut(s: string): string {
  return s.replace(/ü|u:/g, 'v').replace(/Ü|U:/g, 'V')
}

/**
 * Chuẩn hoá chuỗi pinyin số: trim, gộp khoảng trắng, `ü`/`u:` ⇒ `v`, mỗi token phải khớp `^[A-Za-z]+[1-5]$`;
 * giữ chữ hoa ở chữ cái đầu mỗi âm tiết, phần còn lại về chữ thường. Sai bất kỳ token nào ⇒ `null`.
 * Tương đương `PinyinText.NormalizeNumbered` phía backend.
 */
export function normalizeNumbered(input: string): string | null {
  const tokens = replaceUmlaut(input.normalize('NFC')).trim().split(/\s+/).filter(Boolean)
  if (tokens.length === 0) return null
  const out: string[] = []
  for (const token of tokens) {
    const m = NUMBERED_TOKEN.exec(token)
    if (!m) return null
    const letters = m[1]!
    out.push(letters[0]! + letters.slice(1).toLowerCase() + m[2]!)
  }
  return out.join(' ')
}

/** `'Lv4'` ⇒ `{ letters: 'lv', tone: 4, capitalized: true }`; token không đúng dạng số thanh ⇒ `null`. */
export function parseSyllable(token: string): ParsedSyllable | null {
  const m = NUMBERED_TOKEN.exec(replaceUmlaut(token.normalize('NFC').trim()))
  if (!m) return null
  const raw = m[1]!
  return {
    letters: raw.toLowerCase(),
    tone: Number(m[2]) as Tone,
    capitalized: raw[0] !== raw[0]!.toLowerCase(),
  }
}

export function toneOf(token: string): Tone | null {
  return parseSyllable(token)?.tone ?? null
}

/** `'lv4'` ⇒ `'lv'`; token không có số ⇒ trả nguyên (đã trim). */
export function stripTone(token: string): string {
  const parsed = parseSyllable(token)
  return parsed ? parsed.letters : token.trim()
}

/**
 * Vị trí nguyên âm nhận dấu theo quy tắc chuẩn: có `a` ⇒ `a`; không `a` mà có `e` ⇒ `e`; có `ou` ⇒ `o`;
 * còn lại ⇒ nguyên âm CUỐI (`gui4 → guì`, `liu2 → liú`, `huo3 → huǒ`). Không có nguyên âm ⇒ -1.
 */
function markIndex(letters: string): number {
  const a = letters.indexOf('a')
  if (a >= 0) return a
  const e = letters.indexOf('e')
  if (e >= 0) return e
  const ou = letters.indexOf('ou')
  if (ou >= 0) return ou
  for (let i = letters.length - 1; i >= 0; i--) {
    if ('aeiouv'.includes(letters[i]!)) return i
  }
  return -1
}

/** Đặt dấu lên một âm tiết đã tách: `letters` thường (`v` = ü), thanh 1–5, tuỳ chọn viết hoa chữ đầu. */
function markLetters(letters: string, tone: Tone, capitalized: boolean): string {
  const chars = letters.split('')
  if (tone !== 5) {
    const idx = markIndex(letters)
    if (idx >= 0) {
      const vowel = chars[idx] as keyof typeof TONE_MARKS
      chars[idx] = TONE_MARKS[vowel][tone - 1]!
    }
  }
  const marked = chars.map((c) => (c === 'v' ? 'ü' : c))
  if (capitalized && marked.length > 0) marked[0] = marked[0]!.toUpperCase() // 'ā'.toUpperCase() === 'Ā' — Unicode xử lý đúng
  return marked.join('')
}

/** `'lve4'` ⇒ `'lüè'`; token sai dạng ⇒ trả NGUYÊN VĂN (không ném). */
export function syllableToMarked(token: string): string {
  const parsed = parseSyllable(token)
  if (!parsed) return token
  return markLetters(parsed.letters, parsed.tone, parsed.capitalized)
}

export interface NumberedToMarkedOptions {
  /**
   * `true` ⇒ nối liền các âm tiết thành một từ, chèn dấu nháy `'` khi âm tiết sau bắt đầu bằng a/o/e
   * (`Xi1 an1` ⇒ `Xī'ān`). Mặc định cách nhau bằng khoảng trắng.
   */
  join?: boolean
}

/**
 * Dấu câu có thể dính đầu/cuối một âm tiết trong pinyin của hội thoại/ví dụ (F9): ASCII `!?,.;:"'()` và toàn khổ
 * `，。！？、；：“”‘’…（）`. KHÔNG gồm `'` giữa âm tiết của dạng nối (`Xī'ān`) — chỉ cắt ở hai đầu token.
 */
const PUNCT_CLASS = '!?,.;:"\'()\\-–—…，。！？、；：“”‘’（）'
const TOKEN_PUNCT_RE = new RegExp(`^([${PUNCT_CLASS}]*)(.*?)([${PUNCT_CLASS}]*)$`)
const PUNCT_ONLY_RE = new RegExp(`^[${PUNCT_CLASS}\\s]+$`)

/** Tách dấu câu hai đầu token: `'hao3!'` ⇒ `{ lead: '', core: 'hao3', trail: '!' }`. */
export function splitPunctuation(token: string): { lead: string; core: string; trail: string } {
  const m = TOKEN_PUNCT_RE.exec(token)
  if (!m) return { lead: '', core: token, trail: '' }
  return { lead: m[1] ?? '', core: m[2] ?? '', trail: m[3] ?? '' }
}

/** Bỏ mọi dấu câu (và khoảng trắng) khỏi chuỗi chữ Hán — để đếm/ánh xạ chữ ↔ âm tiết. */
export function stripPunctuation(text: string): string {
  return text.replace(new RegExp(`[${PUNCT_CLASS}\\s]`, 'g'), '')
}

/**
 * Chuỗi pinyin số ⇒ dạng dấu: `'ni3 hao3'` ⇒ `'nǐ hǎo'`. Âm tiết `r5` (nhi hoá) dính vào âm tiết trước
 * (`'na3 r5'` ⇒ `'nǎr'`, R5-6). Dấu câu dính đầu/cuối âm tiết được giữ nguyên chỗ (`'Ni3 hao3!'` ⇒ `'Nǐ hǎo!'`,
 * `'Lao3 shi1, nin2 hao3 ma5?'` ⇒ `'Lǎo shī, nín hǎo ma?'`). Token không hợp lệ giữ nguyên văn.
 */
export function numberedToMarked(pinyin: string, opts?: NumberedToMarkedOptions): string {
  const tokens = pinyin.normalize('NFC').trim().split(/\s+/).filter(Boolean)
  const parts: { text: string; startsWithAOE: boolean }[] = []
  for (const token of tokens) {
    const { lead, core, trail } = splitPunctuation(token)
    const parsed = core ? parseSyllable(core) : null
    if (parsed && parsed.letters === 'r' && parsed.tone === 5 && parts.length > 0 && !lead) {
      parts[parts.length - 1]!.text += 'r' + trail
      continue
    }
    parts.push({
      text: parsed ? lead + markLetters(parsed.letters, parsed.tone, parsed.capitalized) + trail : token,
      startsWithAOE: parsed && !lead ? 'aoe'.includes(parsed.letters[0] ?? '') : false,
    })
  }
  if (!opts?.join) return parts.map((p) => p.text).join(' ')
  return parts.map((p, i) => (i > 0 && p.startsWithAOE ? `'${p.text}` : p.text)).join('')
}

/**
 * Dạng dấu ⇒ dạng số: `'nǐ hǎo'` ⇒ `'ni3 hao3'`; không dấu ⇒ thanh nhẹ (`'ma'` ⇒ `'ma5'`); tách theo khoảng trắng
 * hoặc dấu nháy (`"Xī'ān"` ⇒ `'Xi1 an1'`); `ü` ⇒ `v`. Token có ký tự lạ (số, dấu câu) ⇒ `null`.
 * Token đã ở dạng số hợp lệ được giữ nguyên (chuẩn hoá hoa/thường).
 */
export function markedToNumbered(marked: string): string | null {
  const tokens = marked
    .normalize('NFC')
    .trim()
    .split(/[\s'’]+/)
    .filter(Boolean)
  if (tokens.length === 0) return null
  const out: string[] = []
  for (const token of tokens) {
    const numbered = NUMBERED_TOKEN.exec(replaceUmlaut(token))
    if (numbered) {
      out.push(numbered[1]![0]! + numbered[1]!.slice(1).toLowerCase() + numbered[2]!)
      continue
    }
    let tone: Tone = 5
    let letters = ''
    for (const ch of token) {
      const hit = MARK_TO_BASE[ch]
      if (hit) {
        if (tone !== 5) return null // hai dấu thanh trong một âm tiết
        tone = hit.tone
        letters += hit.base
      } else if (ch === 'ü') letters += 'v'
      else if (ch === 'Ü') letters += 'V'
      else letters += ch
    }
    if (!/^[A-Za-z]+$/.test(letters)) return null
    out.push(letters[0]! + letters.slice(1).toLowerCase() + tone)
  }
  return out.join(' ')
}

/** Khoá âm tiết của bảng (R5-1, `v` = ü) ⇒ chữ hiển thị: `'nv'` ⇒ `'nü'`, `'lve'` ⇒ `'lüe'`, `'ju'` ⇒ `'ju'`. */
export function displaySyllableKey(key: string): string {
  return key.replace(/v/g, 'ü')
}

export interface SandhiHint {
  /** Chỉ số âm tiết (theo token của chuỗi pinyin số). */
  index: number
  kind: 'third_tone' | 'bu' | 'yi'
  suggestedTone: 2 | 4
  /** Lời gợi ý ngắn tiếng Việt. */
  text: string
}

/**
 * Gợi ý biến điệu CHỈ ĐỂ HIỂN THỊ (R5-5, R-C3 — app vẫn lưu thanh gốc):
 * - Thanh 3 đứng ngay trước thanh 3 ⇒ đọc gần thanh 2; chuỗi ≥ 3 thanh 3 ⇒ mọi âm tiết trừ cuối, kèm "tuỳ ngắt nhịp".
 * - 不 (`bu4`) trước thanh 4 ⇒ bú. 一 (`yi1`) trước thanh 4 ⇒ yí; trước thanh 1/2/3 ⇒ yì; cuối/trước thanh nhẹ ⇒ không.
 * - 不/一 nhận diện bằng CHỮ HÁN cùng vị trí (`hanzi`), không bằng pinyin (`yi1` còn là 衣, 医...). Không có `hanzi`
 *   ⇒ chỉ gợi ý 3-3.
 */
export function sandhiHints(pinyin: string, hanzi?: string): SandhiHint[] {
  // Bỏ dấu câu dính hai đầu âm tiết (`hao3!`, `ma5?`) và token chỉ có dấu câu trước khi đếm — F9 truyền pinyin
  // của cả câu hội thoại.
  const tokens = pinyin
    .normalize('NFC')
    .trim()
    .split(/\s+/)
    .filter((t) => t && !PUNCT_ONLY_RE.test(t))
    .map((t) => splitPunctuation(t).core)
  const parsed = tokens.map((t) => parseSyllable(t))
  const tones = parsed.map((p) => p?.tone ?? null)

  // Ánh xạ chữ Hán ↔ âm tiết theo vị trí (bỏ dấu câu trong chuỗi chữ Hán); `r5` (儿 nhi hoá) có thể có hoặc
  // không có chữ riêng ⇒ thử cả hai cách.
  const chars = hanzi ? Array.from(stripPunctuation(hanzi)) : []
  const nonErhua = parsed.map((p, i) => (p && p.letters === 'r' && p.tone === 5 && i > 0 ? null : i)).filter((i): i is number => i !== null)
  const charAt = (index: number): string | undefined => {
    if (chars.length === 0) return undefined
    if (chars.length === tokens.length) return chars[index]
    if (chars.length === nonErhua.length) {
      const pos = nonErhua.indexOf(index)
      return pos >= 0 ? chars[pos] : undefined
    }
    return undefined
  }

  const hints: SandhiHint[] = []

  // 3-3: tìm các chuỗi thanh 3 liên tiếp.
  let i = 0
  while (i < tones.length) {
    if (tones[i] !== 3) {
      i++
      continue
    }
    let j = i
    while (j < tones.length && tones[j] === 3) j++
    const run = j - i
    if (run >= 2) {
      for (let k = i; k < j - 1; k++) {
        hints.push({
          index: k,
          kind: 'third_tone',
          suggestedTone: 2,
          text: run >= 3 ? 'đọc gần thanh 2 (chuỗi nhiều thanh 3 — tuỳ cách ngắt nhịp)' : 'đọc gần thanh 2 (3-3)',
        })
      }
    }
    i = j
  }

  // 不 / 一 theo chữ Hán.
  for (let k = 0; k < tokens.length - 1; k++) {
    const p = parsed[k]
    if (!p) continue
    const ch = charAt(k)
    const next = tones[k + 1]
    if (ch === '不' && p.letters === 'bu' && p.tone === 4 && next === 4) {
      hints.push({ index: k, kind: 'bu', suggestedTone: 2, text: 'đọc bú (不 trước thanh 4)' })
    } else if (ch === '一' && p.letters === 'yi' && p.tone === 1) {
      if (next === 4) hints.push({ index: k, kind: 'yi', suggestedTone: 2, text: 'đọc yí (一 trước thanh 4)' })
      else if (next === 1 || next === 2 || next === 3)
        hints.push({ index: k, kind: 'yi', suggestedTone: 4, text: 'đọc yì (一 trước thanh 1/2/3)' })
    }
  }

  return hints.sort((a, b) => a.index - b.index)
}
