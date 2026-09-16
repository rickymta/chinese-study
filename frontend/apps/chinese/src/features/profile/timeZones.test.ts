// Test tiện ích múi giờ + thời gian tương đối của `@af/utils` (F4). Đặt ở app vì vitest chỉ cấu hình ở apps/chinese
// (D35: chỉ test hàm thuần, môi trường node) — hàm thuần nên chạy được ở đây.
import { describe, expect, it } from 'vitest'
import {
  formatRelativeTime,
  formatUtcOffset,
  getUtcOffsetMinutes,
  listTimeZoneOptions,
  matchesTimeZoneQuery,
  normalizeTimeZone,
  type TimeZoneOption,
} from '@af/utils'

describe('normalizeTimeZone — quy bí danh CLDR cũ (R4-2)', () => {
  it('Asia/Saigon ⇒ Asia/Ho_Chi_Minh', () => expect(normalizeTimeZone('Asia/Saigon')).toBe('Asia/Ho_Chi_Minh'))
  it('Europe/Kiev ⇒ Europe/Kyiv', () => expect(normalizeTimeZone('Europe/Kiev')).toBe('Europe/Kyiv'))
  it('tên hiện hành giữ nguyên, có trim', () => expect(normalizeTimeZone('  Asia/Tokyo ')).toBe('Asia/Tokyo'))
})

describe('getUtcOffsetMinutes / formatUtcOffset', () => {
  it('Asia/Ho_Chi_Minh = +420 phút ⇒ UTC+07:00', () => {
    expect(getUtcOffsetMinutes('Asia/Ho_Chi_Minh')).toBe(420)
    expect(formatUtcOffset(420)).toBe('UTC+07:00')
  })
  it('UTC ⇒ 0 ⇒ UTC+00:00', () => {
    expect(getUtcOffsetMinutes('UTC')).toBe(0)
    expect(formatUtcOffset(0)).toBe('UTC+00:00')
  })
  it('Asia/Kolkata = +330 ⇒ UTC+05:30; âm ⇒ UTC-03:30', () => {
    expect(getUtcOffsetMinutes('Asia/Kolkata')).toBe(330)
    expect(formatUtcOffset(-210)).toBe('UTC-03:30')
  })
  it('múi giờ lạ ⇒ null', () => expect(getUtcOffsetMinutes('Mars/Olympus')).toBeNull())
})

describe('listTimeZoneOptions', () => {
  const options = listTimeZoneOptions(['Asia/Saigon'])
  it('có Asia/Ho_Chi_Minh với nhãn (UTC+07:00) và không nhân đôi do bí danh', () => {
    const hits = options.filter((o) => o.id === 'Asia/Ho_Chi_Minh')
    expect(hits).toHaveLength(1)
    expect(hits[0]!.label).toBe('(UTC+07:00) Asia/Ho_Chi_Minh')
  })
  it('không còn tên bí danh cũ', () => expect(options.some((o) => o.id === 'Asia/Saigon')).toBe(false))
  it('sắp theo offset tăng dần rồi tên', () => {
    for (let i = 1; i < options.length; i++) {
      const a = options[i - 1]!
      const b = options[i]!
      const oa = a.offsetMinutes ?? Infinity
      const ob = b.offsetMinutes ?? Infinity
      expect(oa <= ob).toBe(true)
      if (oa === ob) expect(a.id.localeCompare(b.id) <= 0).toBe(true)
    }
  })
})

describe('matchesTimeZoneQuery — gõ "Ho_Chi" hay "ho chi" đều ra (tiêu chí F4 #3)', () => {
  const hcm: TimeZoneOption = { id: 'Asia/Ho_Chi_Minh', offsetMinutes: 420, label: '(UTC+07:00) Asia/Ho_Chi_Minh' }
  const tokyo: TimeZoneOption = { id: 'Asia/Tokyo', offsetMinutes: 540, label: '(UTC+09:00) Asia/Tokyo' }
  it.each(['Ho_Chi', 'ho chi', 'HO CHI MINH', 'asia/ho', 'minh ho', 'utc+07', '+07:00'])('"%s" khớp HCM', (q) =>
    expect(matchesTimeZoneQuery(hcm, q)).toBe(true),
  )
  it('không khớp Tokyo với "ho chi"', () => expect(matchesTimeZoneQuery(tokyo, 'ho chi')).toBe(false))
  it('rỗng ⇒ khớp tất cả', () => expect(matchesTimeZoneQuery(tokyo, '   ')).toBe(true))
})

describe('formatRelativeTime (vi)', () => {
  const now = new Date('2026-09-17T10:00:00Z')
  it('dưới 60 giây / tương lai ⇒ vừa xong', () => {
    expect(formatRelativeTime('2026-09-17T09:59:30Z', now)).toBe('vừa xong')
    expect(formatRelativeTime('2026-09-17T10:05:00Z', now)).toBe('vừa xong')
  })
  it('5 phút trước', () => expect(formatRelativeTime('2026-09-17T09:55:00Z', now)).toMatch(/5 phút/))
  it('hôm qua (numeric auto)', () => expect(formatRelativeTime('2026-09-16T10:00:00Z', now).toLowerCase()).toMatch(/hôm qua|1 ngày/))
  it('3 tuần trước', () => expect(formatRelativeTime('2026-08-27T10:00:00Z', now)).toMatch(/3 tuần/))
  it('chuỗi hỏng ⇒ rỗng', () => expect(formatRelativeTime('abc', now)).toBe(''))
})
