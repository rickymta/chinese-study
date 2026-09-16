/**
 * Mã từ loại (theo `complete-hsk-vocabulary`, kiểu ICTCLAS rút gọn) → nhãn tiếng Việt (hợp đồng §5.3.1).
 * Mã KHÔNG có trong bảng (vd `vn`, `b`) ⇒ ẩn — không đoán, không hiện mã thô cho người học.
 */
export const POS_LABELS: Readonly<Record<string, string>> = {
  n: 'danh từ',
  v: 'động từ',
  a: 'tính từ',
  d: 'phó từ',
  r: 'đại từ',
  m: 'số từ',
  q: 'lượng từ',
  p: 'giới từ',
  c: 'liên từ',
  u: 'trợ từ',
  y: 'trợ từ ngữ khí',
  e: 'thán từ',
  t: 'từ chỉ thời gian',
  f: 'từ phương vị',
  s: 'từ chỉ nơi chốn',
  nr: 'tên người',
  ns: 'địa danh',
  nz: 'danh từ riêng',
  i: 'thành ngữ',
  l: 'cụm cố định',
  o: 'từ tượng thanh',
}

/** Nhãn Việt của một mã; mã lạ ⇒ `null`. So khớp không phân biệt hoa/thường, bỏ khoảng trắng thừa. */
export function posLabel(code: string): string | null {
  const key = code.trim().toLowerCase()
  return POS_LABELS[key] ?? null
}

/** Danh sách nhãn Việt (đã loại mã lạ + trùng, giữ thứ tự) từ mảng mã của API. `null`/rỗng ⇒ `[]`. */
export function posLabels(codes: readonly string[] | null | undefined): string[] {
  if (!codes) return []
  const out: string[] = []
  for (const c of codes) {
    const label = posLabel(c)
    if (label && !out.includes(label)) out.push(label)
  }
  return out
}
