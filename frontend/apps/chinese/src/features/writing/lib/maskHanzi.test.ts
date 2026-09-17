import { describe, expect, it } from 'vitest'
import { MASK_CHAR, maskHanzi } from './maskHanzi'

describe('maskHanzi — che chữ đang luyện trong từ', () => {
  it('che đúng chữ, giữ các chữ khác', () => {
    expect(maskHanzi('爱人', '爱')).toBe(`${MASK_CHAR}人`)
    expect(maskHanzi('可爱', '爱')).toBe(`可${MASK_CHAR}`)
  })
  it('chữ xuất hiện nhiều lần đều bị che', () => {
    expect(maskHanzi('人人', '人')).toBe(`${MASK_CHAR}${MASK_CHAR}`)
  })
  it('từ không chứa chữ ⇒ giữ nguyên', () => {
    expect(maskHanzi('你好', '爱')).toBe('你好')
  })
  it('chữ mở rộng B (cặp surrogate) che theo code point, không cắt đôi', () => {
    expect(maskHanzi('𠀀人', '𠀀')).toBe(`${MASK_CHAR}人`)
    expect(maskHanzi('𠀀人', '人')).toBe(`𠀀${MASK_CHAR}`)
  })
  it('hanzi rỗng ⇒ giữ nguyên', () => {
    expect(maskHanzi('爱', '')).toBe('爱')
  })
})
