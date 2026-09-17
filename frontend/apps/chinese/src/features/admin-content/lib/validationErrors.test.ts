import { describe, expect, it } from 'vitest'
import { flattenValidationDetails, listErrorLines, normalizeErrorPath, partitionErrors, stripPrefix } from './validationErrors'

describe('normalizeErrorPath', () => {
  it('hạ chữ hoa đầu mỗi đoạn, bỏ `$.`', () => {
    expect(normalizeErrorPath('Blocks[2].Payload.Lines[0].Pinyin')).toBe('blocks[2].payload.lines[0].pinyin')
    expect(normalizeErrorPath('$.questions[1].options[2].text')).toBe('questions[1].options[2].text')
    expect(normalizeErrorPath('slug')).toBe('slug')
  })
})

describe('flattenValidationDetails', () => {
  it('nhận mảng chuỗi, chuỗi đơn, bỏ giá trị khác', () => {
    expect(
      flattenValidationDetails({
        'blocks[0].payload.paragraphs': ['Cần ít nhất 1 đoạn.'],
        Slug: 'Slug không hợp lệ.',
        wordIds: [{ id: 1 }],
        count: 3,
      }),
    ).toEqual({ 'blocks[0].payload.paragraphs': ['Cần ít nhất 1 đoạn.'], slug: ['Slug không hợp lệ.'] })
  })
  it('đọc `errors` của ValidationProblemDetails', () => {
    expect(flattenValidationDetails({ errors: { Title: ['Bắt buộc.'] } })).toEqual({ title: ['Bắt buộc.'] })
  })
  it('không có ⇒ rỗng', () => {
    expect(flattenValidationDetails(undefined)).toEqual({})
    expect(flattenValidationDetails(null)).toEqual({})
  })
})

describe('stripPrefix / partitionErrors / listErrorLines', () => {
  const errors = {
    'blocks[2].payload.lines[0].pinyin': ['Sai pinyin.'],
    'blocks[2].payload.title': ['Quá dài.'],
    'blocks[0].payload.text': ['Rỗng.'],
    blocks: ['Tối đa 30 khối.'],
  }
  it('stripPrefix chỉ giữ khoá bắt đầu bằng tiền tố', () => {
    expect(stripPrefix(errors, 'blocks[2].payload.')).toEqual({ 'lines[0].pinyin': ['Sai pinyin.'], title: ['Quá dài.'] })
  })
  it('partitionErrors tách theo isKnown', () => {
    const { mapped, unmapped } = partitionErrors(errors, (p) => p.startsWith('blocks['))
    expect(Object.keys(mapped)).toHaveLength(3)
    expect(unmapped).toEqual({ blocks: ['Tối đa 30 khối.'] })
  })
  it('listErrorLines gộp thành dòng', () => {
    expect(listErrorLines({ blocks: ['Tối đa 30 khối.'], '': ['Chung.'] })).toEqual(['blocks: Tối đa 30 khối.', 'Chung.'])
  })
})
