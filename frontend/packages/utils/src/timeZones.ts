// Tiện ích múi giờ IANA dùng chung mọi app (F4, hợp đồng §5.3.E) — hàm THUẦN, không React, test được bằng vitest.

/**
 * Bí danh CLDR cũ → tên IANA hiện hành (R4-2). Một số bản Chrome trả tên cũ từ `Intl.supportedValuesOf` /
 * `resolvedOptions().timeZone`; backend (.NET `TryFindSystemTimeZoneById`) nhận cả hai nhưng ta gửi tên hiện hành
 * để dữ liệu nhất quán.
 */
const TIME_ZONE_ALIASES: Readonly<Record<string, string>> = {
  'Asia/Saigon': 'Asia/Ho_Chi_Minh',
  'Asia/Calcutta': 'Asia/Kolkata',
  'Asia/Katmandu': 'Asia/Kathmandu',
  'Asia/Rangoon': 'Asia/Yangon',
  'Europe/Kiev': 'Europe/Kyiv',
}

/** Quy bí danh cũ về tên hiện hành; tên khác giữ nguyên (đã trim). */
export function normalizeTimeZone(id: string): string {
  const trimmed = id.trim()
  return TIME_ZONE_ALIASES[trimmed] ?? trimmed
}

/**
 * Danh sách dự phòng (~30 múi giờ phổ biến) khi trình duyệt thiếu `Intl.supportedValuesOf` (Safari < 15.4,
 * Firefox cũ). Luôn có `Asia/Ho_Chi_Minh`.
 */
export const FALLBACK_TIME_ZONES: readonly string[] = [
  'Pacific/Honolulu',
  'America/Anchorage',
  'America/Los_Angeles',
  'America/Denver',
  'America/Chicago',
  'America/New_York',
  'America/Toronto',
  'America/Sao_Paulo',
  'Atlantic/Azores',
  'Europe/London',
  'Europe/Paris',
  'Europe/Berlin',
  'Europe/Madrid',
  'Europe/Rome',
  'Europe/Warsaw',
  'Europe/Kyiv',
  'Europe/Moscow',
  'Asia/Dubai',
  'Asia/Karachi',
  'Asia/Kolkata',
  'Asia/Dhaka',
  'Asia/Yangon',
  'Asia/Bangkok',
  'Asia/Ho_Chi_Minh',
  'Asia/Jakarta',
  'Asia/Singapore',
  'Asia/Shanghai',
  'Asia/Hong_Kong',
  'Asia/Taipei',
  'Asia/Manila',
  'Asia/Seoul',
  'Asia/Tokyo',
  'Australia/Perth',
  'Australia/Sydney',
  'Pacific/Auckland',
  'UTC',
]

/** Múi giờ của thiết bị theo `Intl` (đã quy bí danh); không đọc được ⇒ `fallback`. */
export function detectBrowserTimeZone(fallback = 'Asia/Ho_Chi_Minh'): string {
  try {
    const tz = Intl.DateTimeFormat().resolvedOptions().timeZone
    return tz ? normalizeTimeZone(tz) : fallback
  } catch {
    return fallback
  }
}

/**
 * Độ lệch UTC (phút) của múi giờ tại thời điểm `at` — lấy từ `timeZoneName: 'longOffset'` (`GMT+07:00`);
 * `GMT` không đuôi ⇒ 0. Múi giờ lạ ⇒ `null`.
 */
export function getUtcOffsetMinutes(timeZone: string, at: Date = new Date()): number | null {
  try {
    const parts = new Intl.DateTimeFormat('en-US', { timeZone, timeZoneName: 'longOffset' }).formatToParts(at)
    const name = parts.find((p) => p.type === 'timeZoneName')?.value ?? ''
    if (name === 'GMT' || name === 'UTC') return 0
    const m = /^(?:GMT|UTC)([+-])(\d{1,2})(?::(\d{2}))?$/.exec(name)
    if (!m) return null
    const sign = m[1] === '-' ? -1 : 1
    return sign * (Number(m[2]) * 60 + Number(m[3] ?? 0))
  } catch {
    return null
  }
}

/** `420` ⇒ `UTC+07:00`, `-210` ⇒ `UTC-03:30`, `0` ⇒ `UTC+00:00`. */
export function formatUtcOffset(minutes: number): string {
  const sign = minutes < 0 ? '-' : '+'
  const abs = Math.abs(minutes)
  const h = String(Math.floor(abs / 60)).padStart(2, '0')
  const mm = String(abs % 60).padStart(2, '0')
  return `UTC${sign}${h}:${mm}`
}

export interface TimeZoneOption {
  /** ID IANA (đã quy bí danh). */
  id: string
  /** Độ lệch UTC (phút) tại lúc dựng danh sách; `null` khi không xác định (xếp cuối). */
  offsetMinutes: number | null
  /** Nhãn hiển thị `(UTC+07:00) Asia/Ho_Chi_Minh`. */
  label: string
}

function toOption(id: string, at: Date): TimeZoneOption {
  const offsetMinutes = getUtcOffsetMinutes(id, at)
  const prefix = offsetMinutes === null ? '(UTC ?)' : `(${formatUtcOffset(offsetMinutes)})`
  return { id, offsetMinutes, label: `${prefix} ${id}` }
}

/**
 * Danh sách múi giờ để chọn: `Intl.supportedValuesOf('timeZone')` (thiếu ⇒ `FALLBACK_TIME_ZONES`) → quy bí danh →
 * gộp thêm `extra` (vd giá trị đang lưu của tài khoản, để luôn hiện được) → bỏ trùng → sắp theo offset rồi tên.
 */
export function listTimeZoneOptions(extra: readonly string[] = [], at: Date = new Date()): TimeZoneOption[] {
  let ids: string[]
  try {
    ids = typeof Intl.supportedValuesOf === 'function' ? Intl.supportedValuesOf('timeZone') : [...FALLBACK_TIME_ZONES]
  } catch {
    ids = [...FALLBACK_TIME_ZONES]
  }
  const seen = new Set<string>()
  const out: TimeZoneOption[] = []
  for (const raw of [...ids, ...extra]) {
    const id = normalizeTimeZone(raw)
    if (!id || seen.has(id)) continue
    seen.add(id)
    out.push(toOption(id, at))
  }
  out.sort((a, b) => {
    const oa = a.offsetMinutes ?? Number.POSITIVE_INFINITY
    const ob = b.offsetMinutes ?? Number.POSITIVE_INFINITY
    return oa - ob || a.id.localeCompare(b.id)
  })
  return out
}

/** Chuẩn hoá để so khớp: thường hoá, `_`/`/`/`-` thành khoảng trắng, gộp khoảng trắng. */
function foldForSearch(s: string): string {
  return s.toLowerCase().replace(/[_/-]+/g, ' ').replace(/\s+/g, ' ').trim()
}

/**
 * So khớp không phân biệt hoa thường, coi `_` như khoảng trắng: gõ "Ho_Chi", "ho chi", "ho chi minh", "utc+07",
 * "asia/ho" đều khớp `(UTC+07:00) Asia/Ho_Chi_Minh`. Chuỗi rỗng ⇒ khớp tất cả.
 */
export function matchesTimeZoneQuery(option: TimeZoneOption, query: string): boolean {
  const q = foldForSearch(query)
  if (!q) return true
  const hay = foldForSearch(option.label)
  // Mỗi từ trong câu hỏi phải xuất hiện (thứ tự tự do) — "minh ho chi" vẫn ra.
  return q.split(' ').every((word) => hay.includes(word))
}
