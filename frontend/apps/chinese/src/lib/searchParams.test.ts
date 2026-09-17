import { describe, expect, it } from 'vitest'
import { enumParam, patchSearchParams } from './searchParams'

describe('patchSearchParams', () => {
  it('đặt/xoá theo patch, giữ khoá khác, không đổi bản gốc', () => {
    const prev = new URLSearchParams('q=abc&page=3&hsk=1')
    const out = patchSearchParams(prev, { 'trang-thai': 'nhap', page: null })
    expect(out.toString()).toBe('q=abc&hsk=1&trang-thai=nhap')
    expect(prev.toString()).toBe('q=abc&page=3&hsk=1')
  })
  it('số ⇒ chuỗi; rỗng/undefined ⇒ xoá', () => {
    const out = patchSearchParams(new URLSearchParams('a=1&b=2'), { a: 5, b: '', c: undefined })
    expect(out.toString()).toBe('a=5')
  })
  it('đổi bộ lọc và về trang 1 trong MỘT lần — không mất giá trị lọc', () => {
    const prev = new URLSearchParams('page=4')
    const out = patchSearchParams(prev, { hsk: enumParam('2', '1'), page: null })
    expect(out.get('hsk')).toBe('2')
    expect(out.has('page')).toBe(false)
  })
})

describe('enumParam', () => {
  it('mặc định ⇒ null, khác ⇒ giữ', () => {
    expect(enumParam('tat-ca', 'tat-ca')).toBeNull()
    expect(enumParam('nhap', 'tat-ca')).toBe('nhap')
  })
})
