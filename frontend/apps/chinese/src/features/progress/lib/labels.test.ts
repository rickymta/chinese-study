import { describe, expect, it } from 'vitest'
import { isBrowserTimeZoneDifferent, longestLabel, streakMessage, todayHeading, toPercent } from './labels'

describe('todayHeading', () => {
  it('theo localDate của server, không phụ thuộc đồng hồ trình duyệt', () => {
    expect(todayHeading('2026-09-17')).toBe('Hôm nay, Thứ Năm 17/09')
    expect(todayHeading('2026-09-20')).toBe('Hôm nay, Chủ nhật 20/09')
    expect(todayHeading('2027-01-01')).toBe('Hôm nay, Thứ Sáu 01/01')
  })
  it('chuỗi sai ⇒ chỉ "Hôm nay"', () => {
    expect(todayHeading('')).toBe('Hôm nay')
  })
})

describe('streakMessage / longestLabel', () => {
  it('chưa học hôm nay', () => {
    expect(streakMessage({ current: 0, longest: 0, studiedToday: false })).toBe('Học 1 hoạt động hôm nay để bắt đầu chuỗi.')
    expect(streakMessage({ current: 4, longest: 9, studiedToday: false })).toBe('Học 1 hoạt động để giữ chuỗi.')
  })
  it('đã học hôm nay', () => {
    expect(streakMessage({ current: 1, longest: 1, studiedToday: true })).toBe('Hôm nay đã học — chuỗi bắt đầu!')
    expect(streakMessage({ current: 5, longest: 12, studiedToday: true })).toBe('Hôm nay đã học — chuỗi được giữ.')
  })
  it('nhãn chuỗi dài nhất', () => {
    expect(longestLabel({ current: 0, longest: 0, studiedToday: false })).toBe('Chưa có chuỗi nào')
    expect(longestLabel({ current: 2, longest: 10, studiedToday: true })).toBe('Dài nhất: 10 ngày')
    expect(longestLabel({ current: 10, longest: 10, studiedToday: true })).toContain('Kỷ lục: 10 ngày')
  })
})

describe('isBrowserTimeZoneDifferent', () => {
  it('so sánh không phân biệt hoa thường; thiếu dữ liệu ⇒ không nhắc', () => {
    expect(isBrowserTimeZoneDifferent('Asia/Ho_Chi_Minh', 'Asia/Ho_Chi_Minh')).toBe(false)
    expect(isBrowserTimeZoneDifferent('Asia/Ho_Chi_Minh', 'asia/ho_chi_minh')).toBe(false)
    expect(isBrowserTimeZoneDifferent('America/New_York', 'Asia/Ho_Chi_Minh')).toBe(true)
    expect(isBrowserTimeZoneDifferent('America/New_York', undefined)).toBe(false)
    expect(isBrowserTimeZoneDifferent('', 'Asia/Ho_Chi_Minh')).toBe(false)
  })
  it('bí danh cũ (Chrome trả Asia/Saigon) coi như cùng múi giờ', () => {
    expect(isBrowserTimeZoneDifferent('Asia/Ho_Chi_Minh', 'Asia/Saigon')).toBe(false)
    expect(isBrowserTimeZoneDifferent('Asia/Saigon', 'Asia/Ho_Chi_Minh')).toBe(false)
    expect(isBrowserTimeZoneDifferent('America/Los_Angeles', 'Asia/Saigon')).toBe(true)
  })
})

describe('toPercent', () => {
  it('làm tròn và kẹp 0..100', () => {
    expect(toPercent(0.82)).toBe(82)
    expect(toPercent(0.825)).toBe(83)
    expect(toPercent(1.2)).toBe(100)
    expect(toPercent(null)).toBeNull()
    expect(toPercent(undefined)).toBeNull()
  })
})
