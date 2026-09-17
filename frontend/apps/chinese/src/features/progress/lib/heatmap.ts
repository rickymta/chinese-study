import type { ActivityDay } from '../types'
import { addDays, dateFromDayNumber, dayNumber, mondayIndex, monthOf } from './dates'

/** Mức màu ô lịch (R-PG6 + §5.3.4): 0 · 1–9 · 10–29 · 30–59 · ≥ 60 lượt. */
export type HeatLevel = 0 | 1 | 2 | 3 | 4

export interface HeatmapCell {
  date: string
  count: number
  level: HeatLevel
  isToday: boolean
}

export interface HeatmapMonthLabel {
  /** Chỉ số cột (tuần) đặt nhãn. */
  weekIndex: number
  /** `T9`, `T10`… */
  label: string
}

export interface HeatmapGrid {
  /** Mỗi cột là một tuần Thứ Hai → Chủ nhật (7 phần tử); ô ngoài khoảng dữ liệu ⇒ `null`. */
  weeks: (HeatmapCell | null)[][]
  monthLabels: HeatmapMonthLabel[]
  /** Tổng lượt trong khoảng. */
  total: number
  /** Số ngày có ≥ 1 lượt. */
  activeDays: number
}

export function levelOf(count: number): HeatLevel {
  if (count <= 0) return 0
  if (count < 10) return 1
  if (count < 30) return 2
  if (count < 60) return 3
  return 4
}

/** Số ngày mặc định của lịch khi server trả ít hơn (backend luôn trả 90 — R-PG6). */
const DEFAULT_SPAN = 90

/**
 * Dựng lưới lịch hoạt động từ `activity` (ngày `yyyy-MM-dd` theo múi giờ hồ sơ) và `today` (`localDate` của server).
 * - Khoảng hiển thị `[start, today]` với `start` = ngày sớm nhất trong dữ liệu (hoặc `today − 89` khi dữ liệu trống).
 * - Cột = tuần bắt đầu Thứ Hai; ô trước `start` hoặc sau `today` ⇒ `null` (vẽ trống). 90 ngày ⇒ 13–14 cột.
 * - Ngày trùng trong dữ liệu được cộng dồn; ngày sai định dạng hoặc ngoài khoảng bị bỏ qua.
 * - Nhãn tháng đặt ở cột đầu và mỗi cột mà tháng của ô Thứ Hai (hoặc ô đầu tiên có dữ liệu) đổi so với cột trước.
 */
export function buildHeatmap(activity: readonly ActivityDay[], today: string): HeatmapGrid {
  const todayN = dayNumber(today)
  if (todayN === null) return { weeks: [], monthLabels: [], total: 0, activeDays: 0 }

  const counts = new Map<number, number>()
  let minN = todayN
  for (const day of activity) {
    const n = dayNumber(day.date)
    if (n === null || n > todayN) continue
    counts.set(n, (counts.get(n) ?? 0) + Math.max(0, day.count))
    if (n < minN) minN = n
  }
  const startN = activity.length === 0 ? todayN - (DEFAULT_SPAN - 1) : minN

  // Lùi về Thứ Hai của tuần chứa `start`, tiến tới Chủ nhật của tuần chứa `today`.
  const gridStartN = startN - mondayIndex(dateFromDayNumber(startN))
  const gridEndN = todayN + (6 - mondayIndex(today))

  const weeks: (HeatmapCell | null)[][] = []
  let total = 0
  let activeDays = 0
  for (let weekStart = gridStartN; weekStart <= gridEndN; weekStart += 7) {
    const week: (HeatmapCell | null)[] = []
    for (let i = 0; i < 7; i++) {
      const n = weekStart + i
      if (n < startN || n > todayN) {
        week.push(null)
        continue
      }
      const count = counts.get(n) ?? 0
      total += count
      if (count > 0) activeDays++
      week.push({ date: dateFromDayNumber(n), count, level: levelOf(count), isToday: n === todayN })
    }
    weeks.push(week)
  }

  const monthLabels: HeatmapMonthLabel[] = []
  let prevMonth: number | null = null
  weeks.forEach((week, weekIndex) => {
    const first = week.find((c) => c !== null)
    if (!first) return
    const month = monthOf(first.date)
    if (month === null) return
    if (month !== prevMonth) {
      // Cột đầu chỉ có 1–2 ngày cuối tháng cũ mà cột kế đã sang tháng mới ⇒ hai nhãn sát nhau, bỏ nhãn cột đầu.
      const next = weeks[weekIndex + 1]?.find((c) => c !== null)
      const nextMonth = next ? monthOf(next.date) : null
      if (!(weekIndex === 0 && nextMonth !== null && nextMonth !== month)) {
        monthLabels.push({ weekIndex, label: `T${month}` })
      }
      prevMonth = month
    }
  })

  return { weeks, monthLabels, total, activeDays }
}

/** Ngày đầu tiên của lưới (để test/nhãn): `today − (span − 1)`. */
export function defaultRangeStart(today: string, span = DEFAULT_SPAN): string {
  return addDays(today, -(span - 1))
}
