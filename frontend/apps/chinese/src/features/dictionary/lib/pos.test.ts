import { describe, expect, it } from 'vitest'
import { posLabel, posLabels } from './pos'

describe('posLabel — mã từ loại → nhãn Việt', () => {
  it.each([
    ['n', 'danh từ'],
    ['v', 'động từ'],
    ['a', 'tính từ'],
    ['q', 'lượng từ'],
    ['y', 'trợ từ ngữ khí'],
    ['nr', 'tên người'],
    ['o', 'từ tượng thanh'],
  ])('%s ⇒ %s', (code, label) => {
    expect(posLabel(code)).toBe(label)
  })

  it('không phân biệt hoa/thường và bỏ khoảng trắng', () => {
    expect(posLabel(' N ')).toBe('danh từ')
    expect(posLabel('NR')).toBe('tên người')
  })

  it('mã lạ ⇒ null (ẩn, không đoán)', () => {
    expect(posLabel('vn')).toBeNull()
    expect(posLabel('b')).toBeNull()
    expect(posLabel('')).toBeNull()
  })
})

describe('posLabels — mảng mã của API', () => {
  it('loại mã lạ và mã trùng, giữ thứ tự', () => {
    expect(posLabels(['v', 'vn', 'b', 'n', 'V'])).toEqual(['động từ', 'danh từ'])
  })

  it('null/undefined/rỗng ⇒ []', () => {
    expect(posLabels(null)).toEqual([])
    expect(posLabels(undefined)).toEqual([])
    expect(posLabels([])).toEqual([])
  })
})
