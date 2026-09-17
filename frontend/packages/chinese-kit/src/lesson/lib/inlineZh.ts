/**
 * Cú pháp chữ Hán nội dòng trong học liệu bài học (hợp đồng §5.4.3): `[[<chữ Hán>|<pinyin số thanh>]]`
 * xuất hiện trong `text.paragraphs`, `grammar.explanation/pattern`, `tip.text`, `explanation` của câu hỏi.
 * Backend đã validate khi nạp; parser này VẪN phải khoan dung: token hỏng (thiếu `|`, `[[` không đóng, phần rỗng)
 * giữ nguyên văn bản để không mất chữ khi admin (F10) gõ dở.
 */

export type InlineZhSegment = { kind: 'text'; text: string } | { kind: 'zh'; hanzi: string; pinyin: string }

// `[[` … `|` … `]]` — hai phần không chứa `[`, `]`, `|`, không xuống dòng.
const TOKEN_RE = /\[\[([^[\]|\n]+)\|([^[\]|\n]+)\]\]/g

export function parseInlineZh(text: string): InlineZhSegment[] {
  const out: InlineZhSegment[] = []
  if (!text) return out
  let last = 0
  TOKEN_RE.lastIndex = 0
  let m: RegExpExecArray | null
  while ((m = TOKEN_RE.exec(text))) {
    const hanzi = m[1]!.trim()
    const pinyin = m[2]!.trim()
    // Phần rỗng sau khi bỏ khoảng trắng ⇒ coi là token hỏng, giữ nguyên văn (không tách).
    if (!hanzi || !pinyin) continue
    if (m.index > last) out.push({ kind: 'text', text: text.slice(last, m.index) })
    out.push({ kind: 'zh', hanzi, pinyin })
    last = m.index + m[0].length
  }
  if (last < text.length) out.push({ kind: 'text', text: text.slice(last) })
  return out
}

/** Bỏ mã hoá token, chỉ giữ chữ Hán (`[[你好|ni3 hao3]]` ⇒ `你好`) — dùng cho `aria-label`/tiêu đề thuần chữ. */
export function stripInlineZh(text: string): string {
  return parseInlineZh(text)
    .map((s) => (s.kind === 'zh' ? s.hanzi : s.text))
    .join('')
}
