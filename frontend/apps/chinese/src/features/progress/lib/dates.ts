/**
 * Tiện ích ngày dạng `yyyy-MM-dd` (ngày địa phương theo múi giờ HỒ SƠ do server trả). Mọi phép tính dùng
 * `Date.UTC` + `getUTC*` để trình duyệt ở múi giờ nào cũng ra cùng kết quả — tuyệt đối không `new Date('yyyy-MM-dd')`
 * rồi đọc `getDate()` (lệch một ngày ở múi giờ âm).
 */

const ISO_DATE_RE = /^(\d{4})-(\d{2})-(\d{2})$/

/** `yyyy-MM-dd` ⇒ số ngày kể từ epoch (UTC); chuỗi sai ⇒ `null`. */
export function dayNumber(date: string): number | null {
  const m = ISO_DATE_RE.exec(date)
  if (!m) return null
  const [y, mo, d] = [Number(m[1]), Number(m[2]), Number(m[3])]
  const ms = Date.UTC(y, mo - 1, d)
  // Ngày không tồn tại (31/02) ⇒ Date.UTC tự cuộn sang tháng sau ⇒ coi là sai.
  const back = new Date(ms)
  if (back.getUTCFullYear() !== y || back.getUTCMonth() !== mo - 1 || back.getUTCDate() !== d) return null
  return Math.floor(ms / 86_400_000)
}

/** Ngược lại của `dayNumber`. */
export function dateFromDayNumber(n: number): string {
  const d = new Date(n * 86_400_000)
  return `${d.getUTCFullYear()}-${pad2(d.getUTCMonth() + 1)}-${pad2(d.getUTCDate())}`
}

export function addDays(date: string, days: number): string {
  const n = dayNumber(date)
  if (n === null) return date
  return dateFromDayNumber(n + days)
}

/** 0 = Thứ Hai … 6 = Chủ nhật (tuần bắt đầu Thứ Hai — R-PG6/heatmap). Chuỗi sai ⇒ 0. */
export function mondayIndex(date: string): number {
  const n = dayNumber(date)
  if (n === null) return 0
  // 1970-01-01 là Thứ Năm ⇒ Monday-index 3.
  return (((n + 3) % 7) + 7) % 7
}

/** Tháng (1–12) của ngày; chuỗi sai ⇒ `null`. */
export function monthOf(date: string): number | null {
  const m = ISO_DATE_RE.exec(date)
  return m ? Number(m[2]) : null
}

/** `dd/MM` cho nhãn ngắn (tooltip ô lịch). Chuỗi sai ⇒ trả nguyên văn. */
export function formatDdMm(date: string): string {
  const m = ISO_DATE_RE.exec(date)
  return m ? `${m[3]}/${m[2]}` : date
}

const WEEKDAY_VI = ['Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy', 'Chủ nhật'] as const

/** Tên thứ tiếng Việt theo Monday-index. */
export function weekdayName(date: string): string {
  return WEEKDAY_VI[mondayIndex(date)]
}

/** Nhãn thứ viết tắt cho cột trái của lịch: T2…T7, CN. */
export const WEEKDAY_SHORT_VI = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'] as const

function pad2(n: number): string {
  return n < 10 ? `0${n}` : String(n)
}
