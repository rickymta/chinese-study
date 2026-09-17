import { describe, expect, it } from 'vitest'
import { isLessonSlug, parsePageParam, practicePath, setToHomeSearch, setToTuParam, tabToSet, tuParamToSet } from './setParam'

describe('tabToSet — tab trang chủ ⇒ set API', () => {
  it('ba tab cố định', () => {
    expect(tabToSet('hsk1')).toBe('hsk1')
    expect(tabToSet('can-luyen')).toBe('weak')
    expect(tabToSet('da-luyen')).toBe('practiced')
  })
  it('bai-hoc cần slug hợp lệ', () => {
    expect(tabToSet('bai-hoc', 'chao-hoi')).toBe('lesson:chao-hoi')
    expect(tabToSet('bai-hoc', null)).toBeNull()
    expect(tabToSet('bai-hoc', '')).toBeNull()
    expect(tabToSet('bai-hoc', 'Chao Hoi')).toBeNull()
    expect(tabToSet('bai-hoc', '../x')).toBeNull()
  })
})

describe('setToTuParam ⇄ tuParamToSet', () => {
  it.each(['hsk1', 'weak', 'practiced', 'lesson:chao-hoi'] as const)('vòng tròn %s', (set) => {
    expect(tuParamToSet(setToTuParam(set))).toBe(set)
  })
  it('dạng tham số', () => {
    expect(setToTuParam('weak')).toBe('can-luyen')
    expect(setToTuParam('practiced')).toBe('da-luyen')
    expect(setToTuParam('lesson:so-dem')).toBe('bai:so-dem')
  })
  it('giá trị lạ ⇒ null', () => {
    expect(tuParamToSet(null)).toBeNull()
    expect(tuParamToSet('')).toBeNull()
    expect(tuParamToSet('lesson:abc')).toBeNull()
    expect(tuParamToSet('bai:')).toBeNull()
    expect(tuParamToSet('bai:Không hợp lệ')).toBeNull()
  })
})

describe('setToHomeSearch / practicePath', () => {
  it('hsk1 là mặc định ⇒ không có query', () => {
    expect(setToHomeSearch('hsk1')).toBe('')
    expect(setToHomeSearch(null)).toBe('')
  })
  it('các bộ khác', () => {
    expect(setToHomeSearch('weak')).toBe('?tab=can-luyen')
    expect(setToHomeSearch('practiced')).toBe('?tab=da-luyen')
    expect(setToHomeSearch('lesson:chao-hoi')).toBe('?tab=bai-hoc&bai=chao-hoi')
  })
  it('practicePath encode chữ Hán và gắn tu', () => {
    expect(practicePath('爱')).toBe('/luyen-viet/%E7%88%B1')
    expect(practicePath('爱', 'lesson:chao-hoi')).toBe('/luyen-viet/%E7%88%B1?tu=bai%3Achao-hoi')
    expect(practicePath('爱', null)).toBe('/luyen-viet/%E7%88%B1')
  })
})

describe('isLessonSlug / parsePageParam', () => {
  it('slug', () => {
    expect(isLessonSlug('chao-hoi')).toBe(true)
    expect(isLessonSlug('01-chao-hoi')).toBe(true)
    expect(isLessonSlug('-a')).toBe(false)
    expect(isLessonSlug('a--b')).toBe(false)
    expect(isLessonSlug(undefined)).toBe(false)
  })
  it('page', () => {
    expect(parsePageParam(null)).toBe(1)
    expect(parsePageParam('3')).toBe(3)
    expect(parsePageParam('0')).toBe(1)
    expect(parsePageParam('2.5')).toBe(1)
    expect(parsePageParam('abc')).toBe(1)
  })
})
