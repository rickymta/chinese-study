import { describe, expect, it } from 'vitest'
import { parseInlineZh, stripInlineZh } from './inlineZh'

describe('parseInlineZh', () => {
  it('chuỗi rỗng ⇒ mảng rỗng', () => {
    expect(parseInlineZh('')).toEqual([])
  })

  it('không có token ⇒ một đoạn text nguyên văn', () => {
    expect(parseInlineZh('Xin chào các bạn.')).toEqual([{ kind: 'text', text: 'Xin chào các bạn.' }])
  })

  it('một token giữa câu', () => {
    expect(parseInlineZh('Câu chào là [[你好|ni3 hao3]], dùng được với mọi người.')).toEqual([
      { kind: 'text', text: 'Câu chào là ' },
      { kind: 'zh', hanzi: '你好', pinyin: 'ni3 hao3' },
      { kind: 'text', text: ', dùng được với mọi người.' },
    ])
  })

  it('hai token liền nhau không có text ở giữa', () => {
    expect(parseInlineZh('[[我|wo3]][[们|men5]]')).toEqual([
      { kind: 'zh', hanzi: '我', pinyin: 'wo3' },
      { kind: 'zh', hanzi: '们', pinyin: 'men5' },
    ])
  })

  it('token ở đầu và cuối chuỗi', () => {
    expect(parseInlineZh('[[谢谢|xie4 xie5]] nghĩa là cảm ơn [[吗|ma5]]')).toEqual([
      { kind: 'zh', hanzi: '谢谢', pinyin: 'xie4 xie5' },
      { kind: 'text', text: ' nghĩa là cảm ơn ' },
      { kind: 'zh', hanzi: '吗', pinyin: 'ma5' },
    ])
  })

  it('token thiếu dấu | ⇒ giữ nguyên văn bản', () => {
    expect(parseInlineZh('Chữ [[你好]] không có pinyin')).toEqual([{ kind: 'text', text: 'Chữ [[你好]] không có pinyin' }])
  })

  it('`[[` không đóng ⇒ giữ nguyên văn bản', () => {
    expect(parseInlineZh('Mở [[你好|ni3 hao3 mà không đóng')).toEqual([{ kind: 'text', text: 'Mở [[你好|ni3 hao3 mà không đóng' }])
  })

  it('token có phần rỗng ⇒ giữ nguyên văn, token hợp lệ sau đó vẫn tách', () => {
    expect(parseInlineZh('[[|ni3]] và [[好|hao3]]')).toEqual([
      { kind: 'text', text: '[[|ni3]] và ' },
      { kind: 'zh', hanzi: '好', pinyin: 'hao3' },
    ])
  })

  it('cắt khoảng trắng thừa trong token', () => {
    expect(parseInlineZh('[[ 再见 | zai4 jian4 ]]')).toEqual([{ kind: 'zh', hanzi: '再见', pinyin: 'zai4 jian4' }])
  })

  it('token có xuống dòng bên trong ⇒ không phải token', () => {
    const text = '[[你\n好|ni3 hao3]]'
    expect(parseInlineZh(text)).toEqual([{ kind: 'text', text }])
  })
})

describe('stripInlineZh', () => {
  it('chỉ giữ chữ Hán của token', () => {
    expect(stripInlineZh('Nói [[你好|ni3 hao3]] rồi [[再见|zai4 jian4]].')).toBe('Nói 你好 rồi 再见.')
  })
})
