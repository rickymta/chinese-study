import { normalizeTimeZone } from '@af/utils'
import type { ProgressStreak } from '../types'
import { dayNumber, formatDdMm, weekdayName } from './dates'

/** Tiêu đề trang chủ: `Hôm nay, Thứ Năm 17/09` theo `localDate` của server (không dùng ngày trình duyệt). */
export function todayHeading(localDate: string): string {
  if (dayNumber(localDate) === null) return 'Hôm nay'
  return `Hôm nay, ${weekdayName(localDate)} ${formatDdMm(localDate)}`
}

/** Câu nhắc dưới số chuỗi ngày (StreakCard). */
export function streakMessage(streak: ProgressStreak): string {
  if (streak.studiedToday) {
    return streak.current === 1 ? 'Hôm nay đã học — chuỗi bắt đầu!' : 'Hôm nay đã học — chuỗi được giữ.'
  }
  if (streak.current === 0) return 'Học 1 hoạt động hôm nay để bắt đầu chuỗi.'
  return 'Học 1 hoạt động để giữ chuỗi.'
}

/** Nhãn chuỗi dài nhất; bằng chuỗi hiện tại (và > 0) ⇒ đang ở kỷ lục. */
export function longestLabel(streak: ProgressStreak): string {
  if (streak.longest <= 0) return 'Chưa có chuỗi nào'
  if (streak.current === streak.longest) return `Kỷ lục: ${streak.longest} ngày — đang ở mức cao nhất`
  return `Dài nhất: ${streak.longest} ngày`
}

/**
 * So sánh múi giờ hồ sơ với múi giờ trình duyệt — khác ⇒ trang chủ hiện dòng nhắc (§5.3.4). Quy bí danh CLDR cũ về
 * tên hiện hành trước khi so: Chrome trả `Asia/Saigon` cho máy ở Việt Nam trong khi hồ sơ lưu `Asia/Ho_Chi_Minh` —
 * không quy thì MỌI học viên ở VN đều thấy dòng nhắc oan.
 */
export function isBrowserTimeZoneDifferent(profileTimeZone: string, browserTimeZone: string | undefined): boolean {
  if (!profileTimeZone || !browserTimeZone) return false
  return normalizeTimeZone(profileTimeZone).toLowerCase() !== normalizeTimeZone(browserTimeZone).toLowerCase()
}

/** Múi giờ trình duyệt (IANA); môi trường không hỗ trợ ⇒ `undefined`. */
export function detectBrowserTimeZone(): string | undefined {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone
  } catch {
    return undefined
  }
}

/** `0.82` ⇒ `82`; `null` ⇒ `null`. */
export function toPercent(ratio: number | null | undefined): number | null {
  if (ratio === null || ratio === undefined || Number.isNaN(ratio)) return null
  return Math.round(Math.min(1, Math.max(0, ratio)) * 100)
}
