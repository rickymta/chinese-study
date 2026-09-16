import { describe, expect, it } from 'vitest'
import { computeResponseMs, RESPONSE_MS_MAX, summarizeByTone } from './drillTypes'

describe('computeResponseMs — kẹp trong 0..600000 (validator backend)', () => {
  it('chưa phát ⇒ null', () => expect(computeResponseMs(null, 1000)).toBeNull())
  it('bình thường ⇒ làm tròn ms', () => expect(computeResponseMs(1000, 2800.4)).toBe(1800))
  it('đúng cận trên ⇒ giữ', () => expect(computeResponseMs(0, RESPONSE_MS_MAX)).toBe(RESPONSE_MS_MAX))
  it('quá 10 phút (để yên rồi quay lại) ⇒ null, không gửi giá trị bị 400', () =>
    expect(computeResponseMs(0, RESPONSE_MS_MAX + 1)).toBeNull())
  it('đồng hồ lùi (âm) ⇒ null', () => expect(computeResponseMs(5000, 4000)).toBeNull())
})

describe('summarizeByTone', () => {
  it('đếm theo phần', () => {
    const part = (tone: 1 | 2 | 3 | 4) => ({ syllable: 'ma', hanzi: '妈', tone, meaningVi: '' })
    const out = summarizeByTone([
      { item: { parts: [part(1), part(2)] }, answered: [1, 3], correct: false, responseMs: null, replayCount: 0 },
      { item: { parts: [part(2)] }, answered: [2], correct: true, responseMs: 100, replayCount: 1 },
    ])
    expect(out[1]).toEqual({ total: 1, correct: 1 })
    expect(out[2]).toEqual({ total: 2, correct: 1 })
    expect(out[3]).toEqual({ total: 0, correct: 0 })
  })
})
