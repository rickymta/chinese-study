import { describe, expect, it } from 'vitest'
import { addDays, dateFromDayNumber, dayNumber, formatDdMm, mondayIndex, monthOf, weekdayName } from './dates'

describe('dayNumber / dateFromDayNumber', () => {
  it('đi và về cùng một ngày', () => {
    for (const d of ['1970-01-01', '2026-09-17', '2024-02-29', '1999-12-31']) {
      expect(dateFromDayNumber(dayNumber(d)!)).toBe(d)
    }
    expect(dayNumber('1970-01-01')).toBe(0)
  })

  it('từ chối chuỗi sai định dạng hoặc ngày không tồn tại', () => {
    expect(dayNumber('2026-9-7')).toBeNull()
    expect(dayNumber('2026-02-30')).toBeNull()
    expect(dayNumber('2026-09-17T00:00:00Z')).toBeNull()
    expect(dayNumber('')).toBeNull()
  })
})

describe('addDays', () => {
  it('qua ranh giới tháng, năm và năm nhuận', () => {
    expect(addDays('2026-09-17', 1)).toBe('2026-09-18')
    expect(addDays('2026-12-31', 1)).toBe('2027-01-01')
    expect(addDays('2024-02-28', 1)).toBe('2024-02-29')
    expect(addDays('2026-09-17', -89)).toBe('2026-06-20')
  })
  it('chuỗi sai ⇒ trả nguyên', () => {
    expect(addDays('abc', 3)).toBe('abc')
  })
})

describe('mondayIndex / weekdayName', () => {
  it('2026-09-17 là Thứ Năm, 2026-09-20 là Chủ nhật, 2026-09-21 là Thứ Hai', () => {
    expect(mondayIndex('2026-09-17')).toBe(3)
    expect(weekdayName('2026-09-17')).toBe('Thứ Năm')
    expect(mondayIndex('2026-09-20')).toBe(6)
    expect(weekdayName('2026-09-20')).toBe('Chủ nhật')
    expect(mondayIndex('2026-09-21')).toBe(0)
  })
  it('trước epoch vẫn đúng (1969-12-31 là Thứ Tư)', () => {
    expect(mondayIndex('1969-12-31')).toBe(2)
  })
})

describe('formatDdMm / monthOf', () => {
  it('định dạng dd/MM và lấy tháng', () => {
    expect(formatDdMm('2026-09-07')).toBe('07/09')
    expect(monthOf('2026-12-01')).toBe(12)
    expect(formatDdMm('xyz')).toBe('xyz')
    expect(monthOf('xyz')).toBeNull()
  })
})
