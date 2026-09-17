import { describe, expect, it } from 'vitest'
import { seededRng, shuffle, shuffleOptions } from './shuffle'
import type { QuizQuestion } from '../types'

const question = (id: string, n = 4): QuizQuestion => ({
  id,
  type: 'single_choice',
  prompt: 'p',
  promptLang: 'vi',
  options: Array.from({ length: n }, (_, i) => ({ id: 'abcd'[i]!, text: `opt ${i}`, lang: 'vi' as const })),
})

describe('shuffle', () => {
  it('trả hoán vị: cùng phần tử, cùng số lượng, không sửa mảng vào', () => {
    const input = [1, 2, 3, 4, 5, 6, 7, 8]
    const frozen = Object.freeze(input.slice())
    const out = shuffle(frozen, seededRng(7))
    expect(out).toHaveLength(input.length)
    expect([...out].sort((a, b) => a - b)).toEqual(input)
    expect(frozen).toEqual(input)
  })

  it('cùng seed ⇒ cùng thứ tự (ổn định); seed khác ⇒ có thể khác', () => {
    const input = ['a', 'b', 'c', 'd', 'e', 'f']
    expect(shuffle(input, seededRng(42))).toEqual(shuffle(input, seededRng(42)))
    const many = new Set(Array.from({ length: 20 }, (_, s) => shuffle(input, seededRng(s)).join('')))
    expect(many.size).toBeGreaterThan(1)
  })

  it('mảng rỗng và một phần tử', () => {
    expect(shuffle([], seededRng(1))).toEqual([])
    expect(shuffle(['x'], seededRng(1))).toEqual(['x'])
  })

  it('rng trả 0 ⇒ mọi phần tử đổi chỗ với phần tử đầu (không đọc ngoài mảng); rng trả 1 vẫn an toàn', () => {
    expect(shuffle([1, 2, 3], () => 0)).toEqual([2, 3, 1])
    expect(shuffle([1, 2, 3], () => 1)).toEqual([1, 2, 3])
  })
})

describe('shuffleOptions', () => {
  it('mỗi câu một hoán vị của lựa chọn gốc, khoá theo id câu', () => {
    const qs = [question('q1'), question('q2', 3)]
    const order = shuffleOptions(qs, seededRng(3))
    expect(Object.keys(order)).toEqual(['q1', 'q2'])
    expect(order.q1!.map((o) => o.id).sort()).toEqual(['a', 'b', 'c', 'd'])
    expect(order.q2!.map((o) => o.id).sort()).toEqual(['a', 'b', 'c'])
  })

  it('cùng seed ⇒ cùng thứ tự cho cả bộ câu', () => {
    const qs = [question('q1'), question('q2'), question('q3')]
    const a = shuffleOptions(qs, seededRng(99))
    const b = shuffleOptions(qs, seededRng(99))
    expect(a).toEqual(b)
  })
})
