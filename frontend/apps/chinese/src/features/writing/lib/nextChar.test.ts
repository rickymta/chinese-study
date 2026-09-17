import { describe, expect, it } from 'vitest'
import { findNextPracticable, positionInSet } from './nextChar'

const items = [{ hanzi: '一' }, { hanzi: '二' }, { hanzi: '三' }, { hanzi: '四' }]
const hasData = (h: string) => h !== '三'

describe('findNextPracticable', () => {
  it('chữ kế tiếp có dữ liệu nét', () => {
    expect(findNextPracticable(items, '一', hasData)).toBe('二')
  })
  it('bỏ qua chữ không có dữ liệu nét', () => {
    expect(findNextPracticable(items, '二', hasData)).toBe('四')
  })
  it('chữ cuối ⇒ null (không vòng lại)', () => {
    expect(findNextPracticable(items, '四', hasData)).toBeNull()
  })
  it('chữ hiện tại không trong bộ ⇒ chữ đầu có dữ liệu', () => {
    expect(findNextPracticable(items, '五', hasData)).toBe('一')
    expect(findNextPracticable(items, '五', () => false)).toBeNull()
  })
  it('danh sách rỗng ⇒ null', () => {
    expect(findNextPracticable([], '一', hasData)).toBeNull()
  })
})

describe('positionInSet', () => {
  it('1-based', () => {
    expect(positionInSet(items, '三')).toEqual({ index: 3, total: 4 })
  })
  it('không có ⇒ null', () => {
    expect(positionInSet(items, '五')).toBeNull()
  })
})
