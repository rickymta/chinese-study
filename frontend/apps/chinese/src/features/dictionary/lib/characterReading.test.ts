import { describe, expect, it } from 'vitest'
import { readingAt } from './characterReading'

describe('readingAt — cách đọc của chữ theo vị trí trong từ', () => {
  it('ưu tiên âm tiết của từ khi chữ đa âm', () => {
    // 好 đọc hao3/hao4; trong 你好 là hao3, trong 爱好 là hao4.
    expect(readingAt('ni3 hao3', 1, ['hao3', 'hao4'])).toBe('hao3')
    expect(readingAt('ai4 hao4', 1, ['hao3', 'hao4'])).toBe('hao4')
  })

  it('không khớp ⇒ cách đọc đầu tiên của chữ', () => {
    expect(readingAt('xx1 yy2', 0, ['zhe4', 'zhei4'])).toBe('zhe4')
  })

  it('bỏ âm tiết r5 (nhi hoá) khi đếm vị trí', () => {
    expect(readingAt('na3 r5', 0, ['na3', 'nei3'])).toBe('na3')
    expect(readingAt('yi1 dian3 r5', 1, ['dian3'])).toBe('dian3')
  })

  it('không phân biệt hoa/thường, ü ⇒ v', () => {
    expect(readingAt('Nv3 er2', 0, ['nü3'])).toBe('nv3')
  })

  it('chữ không có cách đọc ⇒ dùng âm tiết trong từ; cả hai thiếu ⇒ null', () => {
    expect(readingAt('ni3 hao3', 0, null)).toBe('ni3')
    expect(readingAt('ni3', 3, [])).toBeNull()
  })
})
