import { describe, expect, it } from 'vitest'
import { generateDrill, TONE_PAIR_COMBOS } from './generateDrill'
import type { PinyinChart, PinyinSyllable, ToneKey } from '../types'

/** Bộ sinh giả tất định (LCG) để test lặp lại được. */
function seeded(seed = 42): () => number {
  let s = seed >>> 0
  return () => {
    s = (s * 1664525 + 1013904223) >>> 0
    return s / 0x100000000
  }
}

/** Catalog giả: 30 âm tiết, mỗi âm tiết đủ 4 thanh, chữ minh hoạ duy nhất. */
function fakeChart(syllableCount = 30): Pick<PinyinChart, 'syllables'> {
  const syllables: PinyinSyllable[] = []
  let code = 0x4e00
  for (let i = 0; i < syllableCount; i++) {
    const tones: PinyinSyllable['tones'] = {}
    for (const t of ['1', '2', '3', '4'] as ToneKey[]) {
      tones[t] = { hanzi: String.fromCharCode(code++), meaningVi: `nghĩa ${i}-${t}` }
    }
    syllables.push({ syllable: `s${i}`, initial: '', final: 'a', tones })
  }
  return { syllables }
}

const chart = fakeChart()
const hanziSet = new Set(chart.syllables.flatMap((s) => Object.values(s.tones).map((t) => t!.hanzi)))

describe('generateDrill — listen_tone', () => {
  it('đủ 20 câu, mỗi câu 1 phần', () => {
    const items = generateDrill({ mode: 'listen_tone', chart, random: seeded() })
    expect(items).toHaveLength(20)
    expect(items.every((it) => it.parts.length === 1)).toBe(true)
  })

  it('không focus ⇒ mỗi thanh đúng 5 câu', () => {
    const items = generateDrill({ mode: 'listen_tone', chart, random: seeded(7) })
    const count = { 1: 0, 2: 0, 3: 0, 4: 0 }
    for (const it of items) count[it.parts[0]!.tone]++
    expect(count).toEqual({ 1: 5, 2: 5, 3: 5, 4: 5 })
  })

  it('focus [2] ⇒ ít nhất 10 câu thanh 2', () => {
    const items = generateDrill({ mode: 'listen_tone', chart, focus: [2], random: seeded(3) })
    expect(items.filter((it) => it.parts[0]!.tone === 2).length).toBeGreaterThanOrEqual(10)
  })

  it('mọi phần có chữ minh hoạ trong chart', () => {
    const items = generateDrill({ mode: 'listen_tone', chart, random: seeded(9) })
    expect(items.every((it) => hanziSet.has(it.parts[0]!.hanzi))).toBe(true)
  })

  it('không lặp chữ khi còn lựa chọn', () => {
    const items = generateDrill({ mode: 'listen_tone', chart, random: seeded(11) })
    const all = items.map((it) => it.parts[0]!.hanzi)
    expect(new Set(all).size).toBe(all.length)
  })

  it('hết lựa chọn thì cho lặp (catalog rất nhỏ) — vẫn đủ câu', () => {
    const tiny = fakeChart(1)
    const items = generateDrill({ mode: 'listen_tone', chart: tiny, random: seeded(1) })
    expect(items).toHaveLength(20)
  })

  it('tất định theo random', () => {
    const a = generateDrill({ mode: 'listen_tone', chart, random: seeded(5) })
    const b = generateDrill({ mode: 'listen_tone', chart, random: seeded(5) })
    expect(a).toEqual(b)
  })
})

describe('generateDrill — tone_pair', () => {
  it('có 15 tổ hợp, không có 3-3', () => {
    expect(TONE_PAIR_COMBOS).toHaveLength(15)
    expect(TONE_PAIR_COMBOS.some(([a, b]) => a === 3 && b === 3)).toBe(false)
  })

  it('đủ 20 câu, mỗi câu 2 phần, không 3-3, hai âm tiết khác nhau, chữ có trong chart', () => {
    const items = generateDrill({ mode: 'tone_pair', chart, random: seeded(21) })
    expect(items).toHaveLength(20)
    for (const it of items) {
      expect(it.parts).toHaveLength(2)
      const [a, b] = it.parts
      expect(a!.tone === 3 && b!.tone === 3).toBe(false)
      expect(a!.syllable).not.toBe(b!.syllable)
      expect(hanziSet.has(a!.hanzi)).toBe(true)
      expect(hanziSet.has(b!.hanzi)).toBe(true)
    }
  })

  it('focus [3] ⇒ ít nhất 10 câu có một phần thanh 3', () => {
    const items = generateDrill({ mode: 'tone_pair', chart, focus: [3], random: seeded(2) })
    expect(items.filter((it) => it.parts.some((p) => p.tone === 3)).length).toBeGreaterThanOrEqual(10)
  })

  it('không lặp chữ khi còn lựa chọn', () => {
    const items = generateDrill({ mode: 'tone_pair', chart, random: seeded(8) })
    const all = items.flatMap((it) => it.parts.map((p) => p.hanzi))
    expect(new Set(all).size).toBe(all.length)
  })
})
