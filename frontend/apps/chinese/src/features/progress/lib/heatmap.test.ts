import { describe, expect, it } from 'vitest'
import type { ActivityDay } from '../types'
import { addDays } from './dates'
import { buildHeatmap, defaultRangeStart, levelOf } from './heatmap'

/** 90 phần tử `[today − 89, today]` như backend trả (R-PG6), `count` theo hàm cho trước. */
function activity90(today: string, countOf: (date: string, i: number) => number = () => 0): ActivityDay[] {
  const start = defaultRangeStart(today)
  return Array.from({ length: 90 }, (_, i) => {
    const date = addDays(start, i)
    return { date, count: countOf(date, i) }
  })
}

describe('levelOf — ngưỡng biên', () => {
  it.each([
    [0, 0],
    [1, 1],
    [9, 1],
    [10, 2],
    [29, 2],
    [30, 3],
    [59, 3],
    [60, 4],
    [500, 4],
  ])('%i lượt ⇒ mức %i', (count, level) => {
    expect(levelOf(count)).toBe(level)
  })
  it('số âm coi như 0', () => {
    expect(levelOf(-3)).toBe(0)
  })
})

describe('buildHeatmap — kích thước lưới', () => {
  it('today là Chủ nhật (2026-09-20) ⇒ 13 cột, cột cuối đủ 7 ô, ô cuối là hôm nay', () => {
    const grid = buildHeatmap(activity90('2026-09-20'), '2026-09-20')
    expect(grid.weeks).toHaveLength(13)
    for (const week of grid.weeks) expect(week).toHaveLength(7)
    const last = grid.weeks[12]!
    expect(last[6]?.date).toBe('2026-09-20')
    expect(last[6]?.isToday).toBe(true)
    // Ngày đầu khoảng 2026-06-23 là Thứ Ba ⇒ ô Thứ Hai của cột đầu trống.
    expect(grid.weeks[0]![0]).toBeNull()
    expect(grid.weeks[0]![1]?.date).toBe('2026-06-23')
  })

  it('today là Thứ Hai (2026-09-21) ⇒ 14 cột, cột cuối chỉ có ô Thứ Hai', () => {
    const grid = buildHeatmap(activity90('2026-09-21'), '2026-09-21')
    expect(grid.weeks).toHaveLength(14)
    const last = grid.weeks[13]!
    expect(last[0]?.date).toBe('2026-09-21')
    expect(last.slice(1).every((c) => c === null)).toBe(true)
  })

  it('today là Thứ Năm (2026-09-17) ⇒ 14 cột (20/06 là Thứ Bảy), các ô sau hôm nay trống', () => {
    const grid = buildHeatmap(activity90('2026-09-17'), '2026-09-17')
    expect(grid.weeks).toHaveLength(14)
    const last = grid.weeks[13]!
    expect(last[3]?.isToday).toBe(true)
    expect(last[4]).toBeNull()
    expect(last[6]).toBeNull()
  })

  it('đúng 90 ô có dữ liệu, còn lại là null', () => {
    const grid = buildHeatmap(activity90('2026-09-17'), '2026-09-17')
    const cells = grid.weeks.flat().filter((c) => c !== null)
    expect(cells).toHaveLength(90)
  })

  it('activity rỗng ⇒ vẫn dựng 90 ngày trống kết thúc ở hôm nay', () => {
    const grid = buildHeatmap([], '2026-09-17')
    const cells = grid.weeks.flat().filter((c) => c !== null)
    expect(cells).toHaveLength(90)
    expect(cells[0]!.date).toBe('2026-06-20')
    expect(cells.at(-1)!.date).toBe('2026-09-17')
    expect(grid.total).toBe(0)
    expect(grid.activeDays).toBe(0)
  })

  it('today sai định dạng ⇒ lưới rỗng, không ném', () => {
    expect(buildHeatmap(activity90('2026-09-17'), 'hôm nay')).toEqual({ weeks: [], monthLabels: [], total: 0, activeDays: 0 })
  })
})

describe('buildHeatmap — số liệu và mức màu', () => {
  it('tính tổng, số ngày có học và mức màu từng ô', () => {
    const today = '2026-09-17'
    const data = activity90(today, (date) => (date === today ? 12 : date === '2026-09-16' ? 1 : 0))
    const grid = buildHeatmap(data, today)
    expect(grid.total).toBe(13)
    expect(grid.activeDays).toBe(2)
    const todayCell = grid.weeks.flat().find((c) => c?.date === today)!
    expect(todayCell.level).toBe(2)
    const yesterday = grid.weeks.flat().find((c) => c?.date === '2026-09-16')!
    expect(yesterday.level).toBe(1)
  })

  it('ngày trùng được cộng dồn; ngày tương lai và chuỗi sai bị bỏ qua', () => {
    const today = '2026-09-17'
    const data: ActivityDay[] = [
      { date: today, count: 5 },
      { date: today, count: 7 },
      { date: '2026-09-18', count: 99 },
      { date: 'không-phải-ngày', count: 99 },
    ]
    const grid = buildHeatmap(data, today)
    const todayCell = grid.weeks.flat().find((c) => c?.date === today)!
    expect(todayCell.count).toBe(12)
    expect(grid.total).toBe(12)
    // Chỉ có 1 ngày dữ liệu ⇒ khoảng là [today, today] ⇒ đúng 1 ô.
    expect(grid.weeks.flat().filter((c) => c !== null)).toHaveLength(1)
  })
})

describe('buildHeatmap — nhãn tháng', () => {
  it('90 ngày 20/06 → 17/09 có nhãn T6, T7, T8, T9 (20/06 là Thứ Bảy ⇒ cột 0–2 thuộc tháng 6)', () => {
    const grid = buildHeatmap(activity90('2026-09-17'), '2026-09-17')
    const labels = grid.monthLabels.map((m) => m.label)
    expect(labels).toEqual(['T6', 'T7', 'T8', 'T9'])
    expect(grid.monthLabels[0]!.weekIndex).toBe(0)
    // Chỉ số cột tăng dần, mỗi nhãn cách nhau ≥ 3 cột để không đè chữ.
    for (let i = 1; i < grid.monthLabels.length; i++) {
      expect(grid.monthLabels[i]!.weekIndex - grid.monthLabels[i - 1]!.weekIndex).toBeGreaterThanOrEqual(3)
    }
  })

  it('cột đầu chỉ có 1–2 ngày cuối tháng cũ mà cột kế đã sang tháng mới ⇒ bỏ nhãn cột đầu', () => {
    // 2026-08-31 là Thứ Hai ⇒ cột 0 mở đầu bằng 31/08 (T8) rồi 01–06/09; cột 1 = 07/09 (T9). Cột 0 chỉ có một ngày
    // của tháng 8 mà cột kế đã sang tháng 9 ⇒ chỉ gắn T9 ở cột 1.
    const data: ActivityDay[] = Array.from({ length: 8 }, (_, i) => ({ date: addDays('2026-08-31', i), count: 0 }))
    const grid = buildHeatmap(data, '2026-09-07')
    expect(grid.monthLabels).toEqual([{ weekIndex: 1, label: 'T9' }])
  })

  it('khoảng bắt đầu đầu tháng ⇒ cột đầu có nhãn tháng đó', () => {
    // 2026-09-01 là Thứ Ba; 5 ngày dữ liệu tới 2026-09-05.
    const data: ActivityDay[] = Array.from({ length: 5 }, (_, i) => ({ date: addDays('2026-09-01', i), count: 0 }))
    const grid = buildHeatmap(data, '2026-09-05')
    expect(grid.monthLabels).toEqual([{ weekIndex: 0, label: 'T9' }])
  })
})
