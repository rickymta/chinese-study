// Chuẩn hoá + kiểm hợp lệ form duyệt nghĩa từ (R-CA9) — hàm thuần, test ở `wordReview.test.ts`.

export const MEANINGS_MAX = 10
export const MEANING_LEN_MAX = 200
export const HAN_VIET_LEN_MAX = 64

/** Trim, bỏ dòng rỗng, bỏ trùng (không phân biệt hoa/thường) giữ thứ tự — giống server để so "có đổi nội dung?". */
export function normalizeMeanings(lines: readonly string[]): string[] {
  const seen = new Set<string>()
  const out: string[] = []
  for (const raw of lines) {
    const m = raw.trim().replace(/\s+/g, ' ')
    if (!m) continue
    const key = m.toLowerCase()
    if (seen.has(key)) continue
    seen.add(key)
    out.push(m)
  }
  return out
}

/** Trim, gộp khoảng trắng, chữ thường; rỗng ⇒ `null` (server lưu `NULL`). */
export function normalizeHanViet(input: string): string | null {
  const s = input.trim().replace(/\s+/g, ' ').toLowerCase()
  return s ? s : null
}

/** Chữ cái Latin + tiếng Việt có dấu (đã NFC), các âm tiết cách nhau đúng một dấu cách. */
const HAN_VIET_RE = /^[a-zàáâãèéêìíòóôõùúýăđĩũơưạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỵỷỹ]+( [a-zàáâãèéêìíòóôõùúýăđĩũơưạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỵỷỹ]+)*$/u

export interface WordEditProblems {
  meaningsVi?: string
  hanViet?: string
}

/** Kiểm sau chuẩn hoá: nghĩa 1–10 mục ≤ 200 ký tự; Hán Việt ≤ 64, chữ thường có dấu, được rỗng. */
export function validateWordEdit(meaningsVi: string[], hanViet: string | null): WordEditProblems {
  const problems: WordEditProblems = {}
  if (meaningsVi.length === 0) problems.meaningsVi = 'Cần ít nhất một nghĩa tiếng Việt.'
  else if (meaningsVi.length > MEANINGS_MAX) problems.meaningsVi = `Tối đa ${MEANINGS_MAX} nghĩa.`
  else if (meaningsVi.some((m) => m.length > MEANING_LEN_MAX)) problems.meaningsVi = `Mỗi nghĩa tối đa ${MEANING_LEN_MAX} ký tự.`
  if (hanViet !== null) {
    if (hanViet.length > HAN_VIET_LEN_MAX) problems.hanViet = `Hán Việt tối đa ${HAN_VIET_LEN_MAX} ký tự.`
    else if (!HAN_VIET_RE.test(hanViet.normalize('NFC')))
      problems.hanViet = 'Hán Việt chỉ gồm chữ thường tiếng Việt có dấu, các âm tiết cách nhau một dấu cách (vd "nhĩ hảo").'
  }
  return problems
}

/** Nội dung nghĩa có đổi so với bản trên server? (server sẽ đặt `meaning_vi_source = manual`). */
export function meaningsChanged(next: readonly string[], current: readonly string[] | null | undefined): boolean {
  const a = normalizeMeanings(next)
  const b = normalizeMeanings(current ?? [])
  return a.length !== b.length || a.some((m, i) => m !== b[i])
}
