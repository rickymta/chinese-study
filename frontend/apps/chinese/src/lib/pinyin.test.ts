import { describe, expect, it } from 'vitest'
import {
  displaySyllableKey,
  markedToNumbered,
  normalizeNumbered,
  numberedToMarked,
  parseSyllable,
  sandhiHints,
  stripTone,
  syllableToMarked,
  toneOf,
} from './pinyin'

describe('syllableToMarked — quy tắc đặt dấu', () => {
  it.each([
    ['lve4', 'lüè'],
    ['gui4', 'guì'],
    ['liu2', 'liú'],
    ['huo3', 'huǒ'],
    ['er2', 'ér'],
    ['r5', 'r'],
    ['zhuang4', 'zhuàng'],
    ['xue2', 'xué'],
    ['jiong3', 'jiǒng'],
    ['ou1', 'ōu'],
    ['lv5', 'lü'],
    ['nv3', 'nǚ'],
    ['A1', 'Ā'],
    ['ma1', 'mā'],
    ['hao3', 'hǎo'],
    ['xie4', 'xiè'],
    ['de5', 'de'],
  ])('%s → %s', (input, expected) => {
    expect(syllableToMarked(input)).toBe(expected)
  })

  it('token sai giữ nguyên văn', () => {
    expect(syllableToMarked('ma')).toBe('ma')
    expect(syllableToMarked('ma6')).toBe('ma6')
    expect(syllableToMarked('')).toBe('')
  })

  it('không nguyên âm (m/n/ng) không ném, trả không dấu', () => {
    expect(syllableToMarked('ng2')).toBe('ng')
    expect(syllableToMarked('m2')).toBe('m')
  })
})

describe('numberedToMarked', () => {
  it('ni3 hao3 → nǐ hǎo', () => {
    expect(numberedToMarked('ni3 hao3')).toBe('nǐ hǎo')
  })
  it('na3 r5 → nǎr (nhi hoá dính âm tiết trước)', () => {
    expect(numberedToMarked('na3 r5')).toBe('nǎr')
  })
  it('giữ chữ hoa: Ou1 zhou1 → Ōu zhōu', () => {
    expect(numberedToMarked('Ou1 zhou1')).toBe('Ōu zhōu')
  })
  it("join: Xi1 an1 → Xī'ān", () => {
    expect(numberedToMarked('Xi1 an1', { join: true })).toBe("Xī'ān")
    expect(numberedToMarked('ni3 hao3', { join: true })).toBe('nǐhǎo')
  })
  it('token sai giữ nguyên, token đúng vẫn chuyển', () => {
    expect(numberedToMarked('ni3 xyz hao3')).toBe('nǐ xyz hǎo')
  })
  it('gộp khoảng trắng thừa', () => {
    expect(numberedToMarked('  ni3   hao3 ')).toBe('nǐ hǎo')
  })
})

describe('normalizeNumbered', () => {
  it('lü4 → lv4', () => expect(normalizeNumbered('lü4')).toBe('lv4'))
  it('lu:4 → lv4', () => expect(normalizeNumbered('lu:4')).toBe('lv4'))
  it('LÜ4 → Lv4 (giữ hoa chữ đầu)', () => expect(normalizeNumbered('LÜ4')).toBe('Lv4'))
  it('gộp khoảng trắng: "ni3  hao3 " → ni3 hao3', () => expect(normalizeNumbered('ni3  hao3 ')).toBe('ni3 hao3'))
  it('Bei3 jing1 giữ hoa', () => expect(normalizeNumbered('Bei3 jing1')).toBe('Bei3 jing1'))
  it('thiếu thanh → null', () => expect(normalizeNumbered('ma')).toBeNull())
  it('thanh 6 → null', () => expect(normalizeNumbered('ma6')).toBeNull())
  it('thanh 0 → null', () => expect(normalizeNumbered('ma0')).toBeNull())
  it('rỗng → null', () => expect(normalizeNumbered('   ')).toBeNull())
})

describe('markedToNumbered', () => {
  it('nǐ hǎo → ni3 hao3', () => expect(markedToNumbered('nǐ hǎo')).toBe('ni3 hao3'))
  it('lǜ → lv4', () => expect(markedToNumbered('lǜ')).toBe('lv4'))
  it('nǚ → nv3', () => expect(markedToNumbered('nǚ')).toBe('nv3'))
  it('ma → ma5 (không dấu = thanh nhẹ)', () => expect(markedToNumbered('ma')).toBe('ma5'))
  it("Xī'ān → Xi1 an1", () => expect(markedToNumbered("Xī'ān")).toBe('Xi1 an1'))
  it('dấu tổ hợp (NFD: u + ̈ + ̌) vẫn đọc được', () => expect(markedToNumbered('nǚ')).toBe('nv3'))
  it('ký tự lạ → null', () => expect(markedToNumbered('ni3!')).toBeNull())
  it('rỗng → null', () => expect(markedToNumbered('')).toBeNull())

  it('round-trip 10 âm tiết số → dấu → số', () => {
    const list = ['ni3', 'hao3', 'lve4', 'gui4', 'liu2', 'zhuang4', 'xue2', 'jiong3', 'er2', 'nv3']
    for (const s of list) expect(markedToNumbered(syllableToMarked(s))).toBe(s)
  })
})

describe('parseSyllable / toneOf / stripTone / displaySyllableKey', () => {
  it('Lv4 → letters lv, tone 4, capitalized', () => {
    expect(parseSyllable('Lv4')).toEqual({ letters: 'lv', tone: 4, capitalized: true })
  })
  it('token sai → null', () => expect(parseSyllable('ma')).toBeNull())
  it('toneOf', () => {
    expect(toneOf('ma3')).toBe(3)
    expect(toneOf('ma')).toBeNull()
  })
  it('stripTone', () => {
    expect(stripTone('lv4')).toBe('lv')
    expect(stripTone('abc')).toBe('abc')
  })
  it('displaySyllableKey', () => {
    expect(displaySyllableKey('nv')).toBe('nü')
    expect(displaySyllableKey('lve')).toBe('lüe')
    expect(displaySyllableKey('ju')).toBe('ju')
  })
})

describe('sandhiHints', () => {
  it('ni3 hao3 → index 0 gợi ý thanh 2', () => {
    const h = sandhiHints('ni3 hao3')
    expect(h).toHaveLength(1)
    expect(h[0]).toMatchObject({ index: 0, kind: 'third_tone', suggestedTone: 2 })
  })
  it('wo3 hen3 hao3 → index 0 và 1', () => {
    expect(sandhiHints('wo3 hen3 hao3').map((h) => h.index)).toEqual([0, 1])
  })
  it('bu4 shi4 + 不是 → bú', () => {
    const h = sandhiHints('bu4 shi4', '不是')
    expect(h).toHaveLength(1)
    expect(h[0]).toMatchObject({ index: 0, kind: 'bu', suggestedTone: 2 })
  })
  it('bu4 hao3 + 不好 → rỗng', () => expect(sandhiHints('bu4 hao3', '不好')).toEqual([]))
  it('yi1 ge4 + 一个 → yí', () => {
    expect(sandhiHints('yi1 ge4', '一个')[0]).toMatchObject({ index: 0, kind: 'yi', suggestedTone: 2 })
  })
  it('yi1 tian1 + 一天 → yì', () => {
    expect(sandhiHints('yi1 tian1', '一天')[0]).toMatchObject({ index: 0, kind: 'yi', suggestedTone: 4 })
  })
  it('yi1 fu5 + 衣服 → rỗng (không phải 一)', () => expect(sandhiHints('yi1 fu5', '衣服')).toEqual([]))
  it('tong3 yi1 + 统一 → rỗng (一 đứng cuối)', () => expect(sandhiHints('tong3 yi1', '统一')).toEqual([]))
  it('không có hanzi → không gợi ý 不/一', () => expect(sandhiHints('bu4 shi4')).toEqual([]))
})
