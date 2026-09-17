import { describe, expect, it } from 'vitest'
import { countHanzi, countPinyinSyllables, isHanziOnly, pinyinProblem } from './zhText'

describe('countHanzi', () => {
  it('đếm chữ Hán, bỏ dấu câu và chữ Latin', () => {
    expect(countHanzi('你好！')).toBe(2)
    expect(countHanzi('我叫 Anna。')).toBe(2)
    expect(countHanzi('')).toBe(0)
    expect(countHanzi('𠀀')).toBe(1) // Ext B — một code point
  })
})

describe('isHanziOnly', () => {
  it('chỉ chữ Hán + dấu câu', () => {
    expect(isHanziOnly('你好！')).toBe(true)
    expect(isHanziOnly('你好 Anna')).toBe(false)
    expect(isHanziOnly('！')).toBe(false)
  })
})

describe('countPinyinSyllables', () => {
  it('bỏ dấu câu, đếm âm tiết', () => {
    expect(countPinyinSyllables('Ni3 hao3!')).toBe(2)
    expect(countPinyinSyllables('lao3 shi1, nin2 hao3 ma5?')).toBe(5)
    expect(countPinyinSyllables('')).toBe(0)
  })
  it('token sai dạng ⇒ null', () => {
    expect(countPinyinSyllables('ni hao')).toBeNull()
    expect(countPinyinSyllables('nǐ hǎo')).toBeNull()
  })
})

describe('pinyinProblem', () => {
  it('hợp lệ ⇒ null', () => {
    expect(pinyinProblem('你好！', 'ni3 hao3!')).toBeNull()
    expect(pinyinProblem('谢谢', 'xie4 xie5')).toBeNull()
  })
  it('báo lỗi rõ ràng', () => {
    expect(pinyinProblem('你好', '')).toMatch(/Chưa nhập/)
    expect(pinyinProblem('你好', 'ni hao')).toMatch(/số thanh/)
    expect(pinyinProblem('你好', 'ni3')).toMatch(/Số âm tiết \(1\) khác số chữ Hán \(2\)/)
  })
})
