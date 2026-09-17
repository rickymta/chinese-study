// Thời gian tương đối tiếng Việt ("5 phút trước", "hôm qua") — hàm thuần, dùng `Intl.RelativeTimeFormat('vi')`.

const UNITS: ReadonlyArray<{ unit: Intl.RelativeTimeFormatUnit; seconds: number }> = [
  { unit: 'year', seconds: 365 * 86_400 },
  { unit: 'month', seconds: 30 * 86_400 },
  { unit: 'week', seconds: 7 * 86_400 },
  { unit: 'day', seconds: 86_400 },
  { unit: 'hour', seconds: 3_600 },
  { unit: 'minute', seconds: 60 },
]

/**
 * `formatRelativeTime('2026-09-17T01:00:00Z', now)` ⇒ "2 giờ trước" / "hôm qua" / "3 tuần trước"; dưới 60 giây ⇒
 * "vừa xong"; thời điểm tương lai (đồng hồ lệch) ⇒ "vừa xong" luôn (không hiện "sau 3 phút" gây khó hiểu).
 * Chuỗi hỏng ⇒ `''`.
 */
export function formatRelativeTime(iso: string | Date | null | undefined, now: Date = new Date()): string {
  if (!iso) return ''
  const t = iso instanceof Date ? iso.getTime() : Date.parse(iso)
  if (!Number.isFinite(t)) return ''
  const diffSeconds = Math.round((t - now.getTime()) / 1000)
  if (diffSeconds > -60) return 'vừa xong'

  let rtf: Intl.RelativeTimeFormat | null = null
  try {
    rtf = new Intl.RelativeTimeFormat('vi', { numeric: 'auto' })
  } catch {
    rtf = null
  }
  for (const { unit, seconds } of UNITS) {
    if (Math.abs(diffSeconds) >= seconds) {
      const value = Math.round(diffSeconds / seconds)
      if (rtf) return rtf.format(value, unit)
      return `${Math.abs(value)} ${LABEL_VI[unit]} trước`
    }
  }
  return 'vừa xong'
}

const LABEL_VI: Record<Intl.RelativeTimeFormatUnit, string> = {
  year: 'năm',
  years: 'năm',
  quarter: 'quý',
  quarters: 'quý',
  month: 'tháng',
  months: 'tháng',
  week: 'tuần',
  weeks: 'tuần',
  day: 'ngày',
  days: 'ngày',
  hour: 'giờ',
  hours: 'giờ',
  minute: 'phút',
  minutes: 'phút',
  second: 'giây',
  seconds: 'giây',
}
