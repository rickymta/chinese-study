import { describe, expect, it } from 'vitest'
import { isValidSlug, slugify, slugProblem } from './slug'

describe('slugify', () => {
  it('bỏ dấu tiếng Việt, đ ⇒ d, chữ thường', () => {
    expect(slugify('Chào hỏi')).toBe('chao-hoi')
    expect(slugify('Đi đường')).toBe('di-duong')
    expect(slugify('Số Đếm 1-10')).toBe('so-dem-1-10')
  })
  it('ký tự lạ ⇒ gạch ngang, gộp và cắt hai đầu', () => {
    expect(slugify('  Gia đình & bạn bè!!  ')).toBe('gia-dinh-ban-be')
    expect(slugify('a___b')).toBe('a-b')
    expect(slugify('你好')).toBe('')
  })
  it('cắt ≤ 64 ký tự, không để gạch ngang cuối', () => {
    const long = Array.from({ length: 30 }, () => 'ab').join(' ')
    const out = slugify(long)
    expect(out.length).toBeLessThanOrEqual(64)
    expect(out.endsWith('-')).toBe(false)
    expect(isValidSlug(out)).toBe(true)
  })
})

describe('isValidSlug / slugProblem', () => {
  it('hợp lệ', () => {
    expect(isValidSlug('chao-hoi')).toBe(true)
    expect(isValidSlug('abc')).toBe(true)
    expect(slugProblem('so-dem')).toBeNull()
  })
  it('không hợp lệ', () => {
    expect(isValidSlug('ab')).toBe(false)
    expect(isValidSlug('Chao-hoi')).toBe(false)
    expect(isValidSlug('chao--hoi')).toBe(false)
    expect(isValidSlug('-chao')).toBe(false)
    expect(slugProblem('ab')).toMatch(/ít nhất/)
    expect(slugProblem('a'.repeat(65))).toMatch(/tối đa/)
    expect(slugProblem('chao hoi')).toMatch(/gạch ngang/)
  })
})
