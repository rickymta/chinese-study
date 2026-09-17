import { describe, expect, it } from 'vitest'
import type { MetaDraft } from './lessonDraft'
import { validateMeta } from './metaValidation'

const good: MetaDraft = {
  title: 'Chào hỏi',
  slug: 'chao-hoi',
  topic: 'giao-tiep',
  orderIndex: 1,
  summary: '',
  objectivesText: 'Chào\nTạm biệt',
  estimatedMinutes: 15,
  glossary: [{ hanzi: '安娜', pinyin: 'an1 na4', vi: 'Anna' }],
}

describe('validateMeta', () => {
  it('bản hợp lệ ⇒ không lỗi', () => {
    expect(validateMeta(good, false, 'chao-hoi')).toEqual({})
  })
  it('slug khoá sau xuất bản — đổi ⇒ lỗi, giữ nguyên ⇒ ok', () => {
    expect(validateMeta({ ...good, slug: 'khac' }, true, 'chao-hoi').slug?.[0]).toMatch(/đã từng xuất bản/)
    expect(validateMeta(good, true, 'chao-hoi').slug).toBeUndefined()
  })
  it('từng trường', () => {
    const e = validateMeta(
      { ...good, title: ' ', topic: 'Giao tiếp', orderIndex: 0, estimatedMinutes: 0, objectivesText: '1\n2\n3\n4\n5\n6\n7' },
      false,
      'chao-hoi',
    )
    expect(e.title).toBeDefined()
    expect(e.topic).toBeDefined()
    expect(e.orderIndex).toBeDefined()
    expect(e.estimatedMinutes).toBeDefined()
    expect(e.objectives?.[0]).toMatch(/Tối đa 6/)
  })
  it('glossary: dòng trống bỏ qua, dòng sai báo theo chỉ số', () => {
    const e = validateMeta(
      { ...good, glossary: [{ hanzi: '', pinyin: '', vi: '' }, { hanzi: 'Anna', pinyin: 'an1', vi: '' }] },
      false,
      'chao-hoi',
    )
    expect(e['glossary[0].hanzi']).toBeUndefined()
    expect(e['glossary[1].hanzi']).toBeDefined()
    expect(e['glossary[1].vi']).toBeDefined()
  })
})
