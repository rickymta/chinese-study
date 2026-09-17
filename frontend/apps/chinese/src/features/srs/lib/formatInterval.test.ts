import { describe, expect, it } from 'vitest'
import { formatInterval, parseDurationSeconds } from './formatInterval'

describe('parseDurationSeconds', () => {
  it('đọc ISO-8601 duration', () => {
    expect(parseDurationSeconds('PT1M')).toBe(60)
    expect(parseDurationSeconds('PT5M30S')).toBe(330)
    expect(parseDurationSeconds('P8D')).toBe(8 * 86_400)
    expect(parseDurationSeconds('P1DT2H')).toBe(86_400 + 7_200)
    expect(parseDurationSeconds('PT0.5S')).toBe(0.5)
  })

  it('đọc TimeSpan .NET (phòng backend trả mặc định)', () => {
    expect(parseDurationSeconds('00:10:00')).toBe(600)
    expect(parseDurationSeconds('8.00:00:00')).toBe(8 * 86_400)
    expect(parseDurationSeconds('00:05:30.5000000')).toBe(330.5)
  })

  it('trả null khi không hiểu', () => {
    expect(parseDurationSeconds('')).toBeNull()
    expect(parseDurationSeconds(null)).toBeNull()
    expect(parseDurationSeconds('P')).toBeNull()
    expect(parseDurationSeconds('10 phút')).toBeNull()
  })
})

describe('formatInterval — ca bắt buộc của hợp đồng', () => {
  it.each([
    ['PT1M', '1 phút'],
    ['PT5M30S', '5,5 phút'],
    ['PT10M', '10 phút'],
    ['PT15M', '15 phút'],
    ['P1D', '1 ngày'],
    ['P8D', '8 ngày'],
    ['P45D', '1,5 tháng'],
    ['P60D', '2 tháng'],
    ['P498D', '1,4 năm'],
  ])('%s → %s', (iso, expected) => {
    expect(formatInterval(iso)).toBe(expected)
  })
})

describe('formatInterval — biên', () => {
  it('giờ và ngày làm tròn số nguyên, tối thiểu 1', () => {
    expect(formatInterval('PT90M')).toBe('2 giờ')
    expect(formatInterval('PT1H')).toBe('1 giờ')
    expect(formatInterval('P1DT12H')).toBe('2 ngày')
    expect(formatInterval('PT23H50M')).toBe('1 ngày')
  })

  it('dưới 10 phút giữ một chữ số thập phân, từ 10 phút làm tròn', () => {
    expect(formatInterval('PT30S')).toBe('0,5 phút')
    expect(formatInterval('PT12M40S')).toBe('13 phút')
    expect(formatInterval('PT59M40S')).toBe('1 giờ')
  })

  it('không hiểu ⇒ gạch ngang', () => {
    expect(formatInterval(undefined)).toBe('—')
    expect(formatInterval('abc')).toBe('—')
  })
})
