import { describe, expect, it } from 'vitest'
import { meaningsChanged, normalizeHanViet, normalizeMeanings, validateWordEdit } from './wordReview'

describe('normalizeMeanings', () => {
  it('trim, bỏ rỗng, bỏ trùng giữ thứ tự', () => {
    expect(normalizeMeanings([' bạn ', '', 'anh', 'Bạn', 'anh  ấy'])).toEqual(['bạn', 'anh', 'anh ấy'])
  })
})

describe('normalizeHanViet', () => {
  it('chữ thường, gộp khoảng trắng, rỗng ⇒ null', () => {
    expect(normalizeHanViet('  Nhĩ   Hảo ')).toBe('nhĩ hảo')
    expect(normalizeHanViet('   ')).toBeNull()
  })
})

describe('validateWordEdit', () => {
  it('hợp lệ ⇒ không lỗi', () => {
    expect(validateWordEdit(['bạn'], 'nhĩ')).toEqual({})
    expect(validateWordEdit(['bạn'], null)).toEqual({})
  })
  it('nghĩa rỗng / quá nhiều / quá dài', () => {
    expect(validateWordEdit([], null).meaningsVi).toMatch(/ít nhất/)
    expect(validateWordEdit(Array.from({ length: 11 }, (_, i) => `n${i}`), null).meaningsVi).toMatch(/Tối đa 10/)
    expect(validateWordEdit(['x'.repeat(201)], null).meaningsVi).toMatch(/200/)
  })
  it('Hán Việt sai dạng', () => {
    expect(validateWordEdit(['bạn'], 'Nhĩ').hanViet).toBeDefined()
    expect(validateWordEdit(['bạn'], 'nhi3').hanViet).toBeDefined()
    expect(validateWordEdit(['bạn'], 'a'.repeat(65)).hanViet).toMatch(/64/)
    expect(validateWordEdit(['bạn'], 'thượng hải').hanViet).toBeUndefined()
  })
})

describe('meaningsChanged', () => {
  it('so sau chuẩn hoá', () => {
    expect(meaningsChanged([' bạn '], ['bạn'])).toBe(false)
    expect(meaningsChanged(['bạn', 'anh'], ['bạn'])).toBe(true)
    expect(meaningsChanged(['anh', 'bạn'], ['bạn', 'anh'])).toBe(true)
    expect(meaningsChanged([], null)).toBe(false)
  })
})
